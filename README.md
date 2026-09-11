# CloudNeko 🐾

Akari — піксельна неко-дівчина (у стилі тамагочі), яка сидить на робочому столі
і показує анімацією, що зараз відбувається:

| Стан      | Коли                                                   | Портрет | Бульбашка |
|-----------|---------------------------------------------------------|---------|-----------|
| `waiting` | чекає на новий запит (простій)                          | сонна (`akari_sleepy`) | "Zzz" |
| `working` | щось робить (виконує інструменти, генерує відповідь)    | зосереджена (`akari_determined`) | анімовані крапки |
| `polling` | чекає на відповідь / дозвіл від користувача              | здивована (`akari_surprised`), легкий нахил голови | "?" |
| `done`    | таску щойно завершено — коротке святкування (~3 сек), потім сама повертається у `waiting` | закохані очі (`akari_love`) | "✓" + стрибає |

Портрети — з безкоштовного паку [Paws & Hearts - Akari Starter Kit](https://iris-game.itch.io/paws-hearts-akari-starter-kit-free-pixel-dating-sim-character)
(ліцензія дозволяє комерційне й некомерційне використання без атрибуції — див. `src/CloudNeko/Assets/Akari/AKARI_LICENSE.txt`).
Плюс окремий портрет `akari_blush` — реакція на гладження (див. нижче).
Хочеш іншу емоцію під якийсь стан — у паку 17 портретів однакового кадрування, досить
підмінити ім'я файлу в `PortraitFiles` (або `PetPortraitFile`) у [MainWindow.xaml.cs](src/CloudNeko/MainWindow.xaml.cs).

Вікно прозоре, без рамки, завжди зверху, не займає місце в панелі задач.
Перетягується лівою кнопкою миші, ПКМ відкриває меню (лише "Закрити").

### Погладити 💗

Подвійний клік по Akari на ~1.6 сек показує рум'яний портрет (`akari_blush`)
і рожеву бульбашку "♥", після чого сама повертається до поточного робочого стану.
Той самий ефект викликається ззовні — через CLI/файл стану (див. нижче), значенням `pet`.

Проєкт складається з **двох** exe:

- **`CloudNeko.exe`** — WPF-віджет (сама Akari на робочому столі). Запускається один раз, довго висить у фоні.
- **`CloudNekoCli.exe`** — крихітна консольна утиліта (без WPF, старт ~20-100 мс) для запису стану
  ззовні. Саме її викликають хуки — вона не відкриває вікон, просто пише файл і завершується.

## Запуск

Готові standalone-файли (не потребують встановленого .NET) — або зібрані самостійно
(див. "Збірка з сурсів" нижче), або завантажені з GitHub Actions:

```
publish\CloudNeko.exe
```

### Автозапуск разом з Windows (опційно)

Створіть ярлик на `publish\CloudNeko.exe` і покладіть його в папку автозавантаження:
`Win+R` → `shell:startup`.

## Збірка з сурсів

Потрібен [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows,
через WPF-залежність — крос-компіляція з Linux/macOS не підтримується).

Запуск без збірки standalone-файлів (для розробки):

```
dotnet run --project src\CloudNeko\CloudNeko.csproj
```

Повна збірка обох standalone-файлів (кладе результат у `publish\`):

```
dotnet publish src\CloudNeko\CloudNeko.csproj -c Release -r win-x64 --self-contained true -o publish
dotnet publish src\CloudNeko.Cli\CloudNeko.Cli.csproj -c Release -r win-x64 --self-contained true -o publish
```

(Навмисно **без** `PublishSingleFile` — самороспаковка single-file exe додавала ~300-600мс
затримки на кожен запуск, що відчутно для `CloudNekoCli.exe`, який хуки викликають на
кожен інструмент. Звичайний self-contained "папкою" стартує в рази швидше.)

### Автоматична збірка (GitHub Actions)

При кожному push у `master` [`.github/workflows/build.yml`](.github/workflows/build.yml)
на `windows-latest` runner збирає обидва exe тим самим способом, що й вище, і завантажує
весь `publish\` як build-артефакт. Забрати готовий білд без встановлення .NET у себе:
GitHub → вкладка **Actions** → останній успішний run → **Artifacts** → `CloudNeko-win-x64`
(zip з обома exe + залежностями, можна одразу розпакувати й запускати на будь-якому Windows).
Workflow можна запустити й вручну через `workflow_dispatch`.

## Керування станом і паралельні сесії

Кожна **сесія** (наприклад, кожен окремий Claude Code) має свій файл:

```
%LOCALAPPDATA%\CloudNeko\sessions\<session_id>.txt
```

CloudNeko слідкує за всією теку (FileSystemWatcher) і показує **агрегований** стан за
пріоритетом `polling` > `working` > `waiting` — тобто якщо хоч одна сесія чекає на
відповідь, буде показано саме це, а до "чекає на запит" неко повертається лише коли
**всі** сесії неактивні. `done` — не стан, а разова подія (як і `pet`): коротке
святкування, після якого відповідна сесія вважається знову `waiting`. Сесії, чий файл не
оновлювався 4+ години, тихо ігноруються в агрегації (захист від "завислих" без `SessionEnd`).

```
CloudNekoCli.exe --set-state waiting  --session <id>
CloudNekoCli.exe --set-state working  --session <id>
CloudNekoCli.exe --set-state polling  --session <id>
CloudNekoCli.exe --set-state done     --session <id>
CloudNekoCli.exe --set-state pet      --session <id>   # разова реакція "погладили"
CloudNekoCli.exe --end-session        --session <id>   # прибрати сесію з агрегації (напр. SessionEnd)
```

`--session` можна не вказувати явно двома способами:
- якщо на stdin прийде JSON з полем `session_id` (саме так Claude Code hooks і передають
  дані — тому в прикладі нижче `--session` взагалі не вказаний, CLI сам його візьме з stdin);
- інакше пише у спільну сесію `_manual` (ручне тестування, менеджмент скрипти без ідеї сесій).

## Інтеграція з Claude Code (опційно)

Щоб неко-тян реагувала на **всі** паралельні сесії Claude Code окремо, повісьте виклики
`CloudNekoCli.exe` на [хуки](https://docs.claude.com/en/docs/claude-code/hooks) у `settings.json`.
Хук-фреймворк сам пайпить JSON (з `session_id`) на stdin команди — CLI прочитає його
самостійно, PowerShell/jq не потрібні. Приклад (шлях підлаштуйте під себе):

```json
{
  "hooks": {
    "SessionStart": [
      { "hooks": [{ "type": "command", "command": "\"D:\\Projects\\cloud-neko\\publish\\CloudNekoCli.exe\" --set-state waiting" }] }
    ],
    "UserPromptSubmit": [
      { "hooks": [{ "type": "command", "command": "\"D:\\Projects\\cloud-neko\\publish\\CloudNekoCli.exe\" --set-state working" }] }
    ],
    "PreToolUse": [
      { "hooks": [{ "type": "command", "command": "\"D:\\Projects\\cloud-neko\\publish\\CloudNekoCli.exe\" --set-state working" }] }
    ],
    "Notification": [
      { "hooks": [{ "type": "command", "command": "\"D:\\Projects\\cloud-neko\\publish\\CloudNekoCli.exe\" --set-state polling" }] }
    ],
    "Stop": [
      { "hooks": [{ "type": "command", "command": "\"D:\\Projects\\cloud-neko\\publish\\CloudNekoCli.exe\" --set-state done" }] }
    ],
    "SessionEnd": [
      { "hooks": [{ "type": "command", "command": "\"D:\\Projects\\cloud-neko\\publish\\CloudNekoCli.exe\" --end-session" }] }
    ]
  }
}
```

## Структура проєкту

```
CloudNeko.sln
src/CloudNeko/                GUI-віджет (WPF)
  App.xaml(.cs)                точка входу
  MainWindow.xaml(.cs)         вікно-віджет: анімаційний цикл, бульбашка, drag, меню
  NekoState.cs                 enum станів + парсер рядків (без WPF-залежностей)
  SessionPaths.cs               шляхи sessions/<id>.txt + санітизація id (без WPF-залежностей)
  StateFileWatcher.cs          стеження за sessions/*.txt + агрегація кількох сесій
  Assets/Akari/                портрети Akari (PNG) + ліцензія паку
src/CloudNeko.Cli/             легкий CLI (без WPF, швидкий старт) для хуків/скриптів
  Program.cs                    --set-state / --end-session, читає session_id зі stdin JSON
```
