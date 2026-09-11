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
Перетягується лівою кнопкою миші. Є ще іконка в системному треї — вона лишається
доступною, навіть коли сама Akari прихована (автопоказ, див. нижче); клік по ній
одразу показує її знову — навіть якщо вона зараз на іншому фізичному моніторі чи
іншому віртуальному робочому столі Windows: вікно переноситься на монітор, де
курсор, і на поточний віртуальний стіл (через `IVirtualDesktopManager`).

### Погладити 💗

Подвійний клік по Akari на ~1.6 сек показує рум'яний портрет (`akari_blush`)
і рожеву бульбашку "♥", після чого сама повертається до поточного робочого стану.
Той самий ефект викликається ззовні — через CLI/файл стану (див. нижче), значенням `pet`.

### Автопоказ і автозапуск

Правою кнопкою по Akari (або по іконці в треї) → два перемикачі, обидва вимкнені
за замовчуванням і запам'ятовуються між запусками:

- **Автопоказ під час роботи** — Akari з'являється, щойно хоч одна сесія починає
  працювати/чекати відповіді (або йде святкування/гладження), і ховається через
  **3 хвилини** після того, як усі сесії замовкли. Поки прихована — трей-іконка
  лишається робочою точкою входу (правий клік → меню, лівий клік → показати,
  на будь-якому моніторі/віртуальному столі). Стан зберігається в
  `%LOCALAPPDATA%\CloudNeko\autohide.txt`.
- **Автозапуск з Windows** — додає/прибирає `CloudNeko.exe` в
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (без потреби в правах
  адміністратора чи ярлика в папці автозавантаження).

Там же є пункт **"Сховати до трею"** — миттєво ховає Akari вручну (на відміну від
автопоказу, тут без 3-хвилинного очікування і незалежно від того, увімкнений
автопоказ чи ні). Повернути — клік по іконці в треї.

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

Автозапуск разом з Windows вмикається через контекстне меню (див. "Автопоказ і
автозапуск" вище) — не треба вручну класти ярлик у `shell:startup`.

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
на `windows-latest` runner збирає обидва exe тим самим способом, що й вище і публікує
[**GitHub Release**](../../releases/latest) з тегом `v0.1.<номер запуску>`
(номер завжди зростає — простий автоінкремент без ручного версіонування) та прикріпленим
`CloudNeko-win-x64.zip` (обидва exe + залежності — розпакувати й одразу запускати на
будь-якому Windows, .NET встановлювати не треба). Стабільне посилання на найновіший білд:

```
https://github.com/Axiks/claud-neko/releases/latest/download/CloudNeko-win-x64.zip
```

Той самий білд додатково лежить і як build-артефакт (Actions → run → Artifacts) —
зручно для перегляду конкретного run без прив'язки до релізів.
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
  TrayIcon.cs                  іконка в системному треї (меню доступне і коли вікно приховане)
  VirtualDesktopInterop.cs     перенос вікна на поточний віртуальний стіл (IVirtualDesktopManager)
  Assets/Akari/                портрети Akari (PNG) + ліцензія паку
src/CloudNeko.Cli/             легкий CLI (без WPF, швидкий старт) для хуків/скриптів
  Program.cs                    --set-state / --end-session, читає session_id зі stdin JSON
```
