# Отчёт по лабораторной работе №6
## Мониторинг пайплайна, Webhooks и Telegram-оповещения

**Студент:** Александр Дятлов  
**По дисциплине:** Архитектура программного обеспечения (АРПО)  
**Репозиторий:** [AlexanderDyatlov/ARPO-LAB1](https://github.com/AlexanderDyatlov/ARPO-LAB1)

---

## 1. Цель работы

Настроить обратную связь от облачного сервера к команде: после каждого прогона пайплайна Telegram-бот отправляет сообщение с результатом. Без этого о падении сборки узнают, только когда сами открывают вкладку Actions.

Webhook (обратный вызов) — push-механизм: система сама отправляет HTTP-запрос на нужный URL при наступлении события, не дожидаясь опроса (polling). Здесь задача GitHub Actions по завершении пайплайна отправляет POST-запрос в API Telegram.

---

## 2. Создание Telegram-бота

1. В Telegram через официального бота `@BotFather` командой `/newbot` создан бот, получен токен вида `123456789:AAH...`.
2. В личном чате с ботом отправлена команда `/start`: бот не может написать первым.
3. Запросом `https://api.telegram.org/bot<TOKEN>/getUpdates` получен идентификатор чата (`result[].message.chat.id`).

> <img width="496" height="760" alt="image" src="https://github.com/user-attachments/assets/72282165-1e51-4f55-86ef-d5220a77b0d9" />


---

## 3. Секреты репозитория

Токен и chat_id являются учётными данными, поэтому в `main.yml` и в файлах проекта их нет. Они хранятся в секретах: `Settings -> Secrets and variables -> Actions`.

| Секрет | Назначение |
| :--- | :--- |
| `TELEGRAM_BOT_TOKEN` | токен бота |
| `TELEGRAM_CHAT_ID` | идентификатор чата получателя |

В логах значения автоматически маскируются звёздочками.

> <img width="742" height="258" alt="image" src="https://github.com/user-attachments/assets/8452fc31-4fba-4c6b-ad4d-91cfe8111588" />


---

## 4. Задача `notify` в пайплайне

Задача включает в `needs` все остальные задачи, а условие `if: always()` запускает её независимо от их результата (аналог `after_script`). Статус каждой задачи читается выражением `${{ needs.<задача>.result }}`.

```yaml
  # ЛР №6: уведомление в Telegram при любом исходе
  notify:
    needs: [sanity_check, run_tests, build_game, mirror_repo, build_docker_image, deploy_to_pages]
    if: always()
    runs-on: ubuntu-latest
    steps:
      - name: Determine pipeline status
        id: status
        env:
          R_SANITY: ${{ needs.sanity_check.result }}
          R_TESTS: ${{ needs.run_tests.result }}
          R_BUILD: ${{ needs.build_game.result }}
          R_MIRROR: ${{ needs.mirror_repo.result }}
          R_DOCKER: ${{ needs.build_docker_image.result }}
          R_PAGES: ${{ needs.deploy_to_pages.result }}
        run: |
          if [ "$R_SANITY" != "success" ]; then echo "stage=sanity_check" >> $GITHUB_OUTPUT
          elif [ "$R_TESTS" != "success" ]; then echo "stage=run_tests" >> $GITHUB_OUTPUT
          elif [ "$R_BUILD" != "success" ]; then echo "stage=build_game" >> $GITHUB_OUTPUT
          elif [ "$R_MIRROR" != "success" ] && [ "$R_MIRROR" != "skipped" ]; then echo "stage=mirror_repo" >> $GITHUB_OUTPUT
          elif [ "$R_DOCKER" != "success" ]; then echo "stage=build_docker_image" >> $GITHUB_OUTPUT
          elif [ "$R_PAGES" != "success" ]; then echo "stage=deploy_to_pages" >> $GITHUB_OUTPUT
          else echo "stage=none" >> $GITHUB_OUTPUT
          fi

      - name: Send Telegram notification
        env:
          BOT_TOKEN: ${{ secrets.TELEGRAM_BOT_TOKEN }}
          CHAT_ID: ${{ secrets.TELEGRAM_CHAT_ID }}
          STAGE: ${{ steps.status.outputs.stage }}
          SITE_URL: "https://alexanderdyatlov.github.io/ARPO-LAB1/"
          RUN_URL: "${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}"
          AUTHOR: ${{ github.event.head_commit.author.name }}
        run: |
          if [ "$STAGE" = "none" ]; then
            TEXT=$(printf 'Сборка ARPO-LAB1 завершена успешно.\nАвтор коммита: %s\nИгра доступна: %s\nПрогон: %s' "$AUTHOR" "$SITE_URL" "$RUN_URL")
          else
            TEXT=$(printf 'Сборка ARPO-LAB1 упала на задаче %s.\nАвтор коммита: %s\nЛог прогона: %s' "$STAGE" "$AUTHOR" "$RUN_URL")
          fi
          curl -s -X POST "https://api.telegram.org/bot${BOT_TOKEN}/sendMessage" \
            --data-urlencode "chat_id=${CHAT_ID}" \
            --data-urlencode "text=${TEXT}"
```

**Отличия от шаблона в методичке.**
- Результаты задач передаются через переменные окружения `R_*`, а не подставляются прямо в скрипт.
- Задача `mirror_repo` учитывается отдельно: статус `skipped` (например, в форке) не считается ошибкой.
- Текст собирается через `printf`, а `curl` использует `--data-urlencode`. Данные уходят как поля формы, а не JSON, но метод `sendMessage` принимает оба формата. Так корректно передаются переводы строк и кириллица.

**Связь с ЛР №5.** Шаг сборки в `build_game` выполняется с `continue-on-error: true`, и задача считается успешной, если билд реально создан (см. ЛР №5, раздел 6). Поэтому при штатной сборке `notify` сообщает об успехе. Если билда нет, шаг `Verify WebGL build exists` падает, и в уведомлении будет указана задача `build_game`.

---

## 5. Проверка сценариев

### 5.1. Успешный сценарий

После пуша и завершения прогона на телефон пришло сообщение с текстом «Сборка ARPO-LAB1 завершена успешно», именем автора коммита, ссылкой на игру и ссылкой на прогон.

> <img width="415" height="149" alt="image" src="https://github.com/user-attachments/assets/413f32e2-77d0-4a86-8245-9a24bb3acd1e" />


### 5.2. Аварийный сценарий

> **Выполнить и описать перед сдачей.** Временно внести ошибку в шаг `Check Unity Directories` (например, заменить `ProjectSettings` на несуществующую папку), сделать пуш и убедиться, что пришло сообщение вида «Сборка ARPO-LAB1 упала на задаче sanity_check» с автором и ссылкой на лог, а сама задача `notify` осталась зелёной. После проверки вернуть исправленный `main.yml` и сделать пуш.
>
> <img width="498" height="636" alt="image" src="https://github.com/user-attachments/assets/e5a3ea03-1328-46e4-958d-ea11ad9d4139" />
<img width="1240" height="416" alt="image" src="https://github.com/user-attachments/assets/fb8c13d0-a9e6-400e-b3d9-e3b4648ba622" />



---

## 6. Ответы на контрольные вопросы

**1. Чем webhook отличается от polling API и какую задачу решает в CI/CD?**
При polling получатель сам регулярно опрашивает систему о статусе, из-за чего возникают задержки и лишние запросы. Webhook работает наоборот: источник сам отправляет запрос при наступлении события. В CI/CD это позволяет мгновенно узнавать о результате сборки, не открывая вкладку Actions.

**2. Почему токен бота и chat_id нельзя хранить в `main.yml` или коде проекта?**
Файл попадает в репозиторий и в историю Git, и любой, кто его увидит, получит доступ к боту: сможет читать переписку и писать от его имени. Секреты репозитория недоступны в исходном коде и маскируются в логах. При утечке токен нужно отозвать командой `/revoke` у `@BotFather`.

**3. Какую роль играют `needs` и `if: always()`? Что будет, если убрать `if: always()`?**
`needs` перечисляет задачи, результаты которых читает `notify`, и заставляет её ждать их завершения. `if: always()` запускает задачу при любом исходе. Без этого условия по умолчанию действует `success()`, и при падении любой зависимости `notify` была бы пропущена, то есть уведомление об ошибке не пришло бы.

**4. Какие поля JSON передаёт `sendMessage` и как `curl` отправляет POST-запрос?**
Обязательные поля: `chat_id` (получатель) и `text` (текст сообщения). `curl -X POST` отправляет запрос на `https://api.telegram.org/bot<TOKEN>/sendMessage`. Для JSON задаются заголовок `Content-Type: application/json` и тело `-d '{"chat_id":"...","text":"..."}'`. В нашем пайплайне те же поля передаются через `--data-urlencode`.
