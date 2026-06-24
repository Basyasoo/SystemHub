using CommunityToolkit.Mvvm.ComponentModel;

namespace SystemHub.Services
{
    public partial class LocalizationService : ObservableObject
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        [ObservableProperty]
        private string _currentLanguage = "RU"; // "RU", "EN", "ZH"

        private LocalizationService() { }

        public void SetLanguage(string lang)
        {
            CurrentLanguage = lang;
            // Notify all property changes
            OnPropertyChanged(string.Empty);
        }

        // Sidebar
        public string SidebarHome => CurrentLanguage switch
        {
            "EN" => "Dashboard",
            "ZH" => "仪表盘",
            _ => "Главная"
        };

        public string SidebarWeather => CurrentLanguage switch
        {
            "EN" => "Weather",
            "ZH" => "天气",
            _ => "Погода"
        };

        public string SidebarSystem => CurrentLanguage switch
        {
            "EN" => "About Computer",
            "ZH" => "关于电脑",
            _ => "О компьютере"
        };

        public string SidebarCleaner => CurrentLanguage switch
        {
            "EN" => "Cleaner",
            "ZH" => "系统清理",
            _ => "Очистка"
        };

        public string SidebarStartup => CurrentLanguage switch
        {
            "EN" => "Startup",
            "ZH" => "开机自启",
            _ => "Автозапуск"
        };

        public string SidebarPlayer => CurrentLanguage switch
        {
            "EN" => "Player",
            "ZH" => "播放器",
            _ => "Плеер"
        };

        public string SidebarAbout => CurrentLanguage switch
        {
            "EN" => "About App",
            "ZH" => "关于应用",
            _ => "О приложении"
        };

        public string SidebarTweaks => CurrentLanguage switch
        {
            "EN" => "Customization",
            "ZH" => "个性化",
            _ => "Кастомизация"
        };

        public string SidebarTools => CurrentLanguage switch
        {
            "EN" => "Tools",
            "ZH" => "工具",
            _ => "Инструменты"
        };

        public string SidebarProfile => CurrentLanguage switch
        {
            "EN" => "Profile",
            "ZH" => "个人中心",
            _ => "Личный кабинет"
        };


        // Dashboard
        public string DashboardLoadTitle => CurrentLanguage switch
        {
            "EN" => "Component Load",
            "ZH" => "组件负载",
            _ => "Нагрузка компонентов"
        };

        public string DashboardCpu => CurrentLanguage switch
        {
            "EN" => "CPU",
            "ZH" => "CPU",
            _ => "ЦП"
        };

        public string DashboardRam => CurrentLanguage switch
        {
            "EN" => "RAM",
            "ZH" => "RAM",
            _ => "ОЗУ"
        };

        public string DashboardTipTitle => CurrentLanguage switch
        {
            "EN" => "System Tip",
            "ZH" => "系统建议",
            _ => "Системный совет"
        };

        public string DashboardTipText => CurrentLanguage switch
        {
            "EN" => "To free up space, go to the 'Cleaner' section to safely delete Windows cache files.",
            "ZH" => "为了释放磁盘空间，请前往“清理”部分安全删除 Windows 缓存文件。",
            _ => "Для освобождения занятого пространства перейдите в раздел «Очистка» для безопасного удаления кэш-файлов Windows."
        };

        public string DashboardSettingsTitle => CurrentLanguage switch
        {
            "EN" => "Interface Settings",
            "ZH" => "界面设置",
            _ => "Настройки интерфейса"
        };

        public string DashboardThemeLabel => CurrentLanguage switch
        {
            "EN" => "Theme",
            "ZH" => "主题",
            _ => "Тема оформления"
        };

        public string DashboardThemeLight => CurrentLanguage switch
        {
            "EN" => "Light",
            "ZH" => "浅色",
            _ => "Светлая"
        };

        public string DashboardThemeDark => CurrentLanguage switch
        {
            "EN" => "Dark",
            "ZH" => "深色",
            _ => "Темная"
        };

        public string DashboardLangLabel => CurrentLanguage switch
        {
            "EN" => "Language",
            "ZH" => "语言",
            _ => "Язык интерфейса"
        };

        // Player
        public string PlayerHeader => CurrentLanguage switch
        {
            "EN" => "Media Player",
            "ZH" => "音乐播放器",
            _ => "Музыкальный плеер"
        };

        public string PlayerActiveSessions => CurrentLanguage switch
        {
            "EN" => "Active Audio Sessions",
            "ZH" => "活动音频会话",
            _ => "Активные аудио-сессии"
        };

        public string PlayerNoActive => CurrentLanguage switch
        {
            "EN" => "No Active Players",
            "ZH" => "未检测到活动播放器",
            _ => "Плееры не запущены"
        };

        public string PlayerLaunchRecommendation => CurrentLanguage switch
        {
            "EN" => "Start Spotify, YouTube, or another player in your browser",
            "ZH" => "请在浏览器或桌面上启动网易云音乐、Spotify、QQ音乐等播放器",
            _ => "Запустите Spotify, Яндекс.Музыку или плеер в браузере"
        };

        public string PlayerControlBtn => CurrentLanguage switch
        {
            "EN" => "Control",
            "ZH" => "控制",
            _ => "Управлять"
        };

        public string YandexMusicLabel => CurrentLanguage switch
        {
            "EN" => "Yandex Music",
            "ZH" => "Yandex Music",
            _ => "Яндекс Музыка"
        };

        public string PlayerAppLabel => CurrentLanguage switch
        {
            "EN" => "Source/App",
            "ZH" => "来源/应用",
            _ => "Сайт/Приложение"
        };

        // Weather
        public string WeatherDesc => CurrentLanguage switch
        {
            "EN" => "Detailed and accurate location-based weather forecast",
            "ZH" => "基于地理位置的详细精准天气预报",
            _ => "Детальный и точный прогноз погоды на основе локации"
        };

        public string WeatherRefresh => CurrentLanguage switch
        {
            "EN" => "Refresh Data",
            "ZH" => "更新数据",
            _ => "Обновить данные"
        };

        public string WeatherHumidity => CurrentLanguage switch
        {
            "EN" => "HUMIDITY",
            "ZH" => "湿度",
            _ => "ВЛАЖНОСТЬ"
        };

        public string WeatherWind => CurrentLanguage switch
        {
            "EN" => "WIND SPEED",
            "ZH" => "风速",
            _ => "СКОРОСТЬ ВЕТРА"
        };

        public string WeatherFeelsLike => CurrentLanguage switch
        {
            "EN" => "FEELS LIKE",
            "ZH" => "体感温度",
            _ => "ОЩУЩАЕТСЯ КАК"
        };

        public string WeatherToday => CurrentLanguage switch
        {
            "EN" => "Today",
            "ZH" => "今天",
            _ => "Сегодня"
        };

        public string WeatherNight => CurrentLanguage switch
        {
            "EN" => " (night)",
            "ZH" => " (夜间)",
            _ => " (ночь)"
        };

        // System
        public string SystemCpu => CurrentLanguage switch
        {
            "EN" => "Processor",
            "ZH" => "处理器",
            _ => "Процессор"
        };

        public string SystemRam => CurrentLanguage switch
        {
            "EN" => "Memory (RAM)",
            "ZH" => "内存 (RAM)",
            _ => "Оперативная память"
        };

        public string SystemOs => CurrentLanguage switch
        {
            "EN" => "Operating System",
            "ZH" => "操作系统",
            _ => "Операционная система"
        };

        public string SystemGpu => CurrentLanguage switch
        {
            "EN" => "Graphics (GPU)",
            "ZH" => "显卡 (GPU)",
            _ => "Видеокарта"
        };

        public string SystemMotherboard => CurrentLanguage switch
        {
            "EN" => "Motherboard",
            "ZH" => "主板",
            _ => "Материнская плата"
        };

        public string SystemDisk => CurrentLanguage switch
        {
            "EN" => "Disk Drive",
            "ZH" => "硬盘",
            _ => "Жесткий диск"
        };

        public string SystemVpn => CurrentLanguage switch
        {
            "EN" => "VPN Status",
            "ZH" => "VPN 状态",
            _ => "Состояние VPN"
        };

        public string SystemVpnOn => CurrentLanguage switch
        {
            "EN" => "Connected",
            "ZH" => "已连接",
            _ => "Подключен"
        };

        public string SystemVpnOff => CurrentLanguage switch
        {
            "EN" => "Disconnected",
            "ZH" => "未连接",
            _ => "Отключен"
        };

        // Cleaner
        public string CleanerDesc => CurrentLanguage switch
        {
            "EN" => "Safely clean temporary files and cache to free up space",
            "ZH" => "安全清理临时文件和缓存以释放空间",
            _ => "Безопасная очистка временных файлов и кэша для освобождения места"
        };

        public string CleanerScan => CurrentLanguage switch
        {
            "EN" => "Scan System",
            "ZH" => "扫描系统",
            _ => "Анализ системы"
        };

        public string CleanerClean => CurrentLanguage switch
        {
            "EN" => "Clean Files",
            "ZH" => "安全清理",
            _ => "Очистить файлы"
        };

        public string CleanerScanCompleted => CurrentLanguage switch
        {
            "EN" => "Scan Completed",
            "ZH" => "扫描完成",
            _ => "Анализ завершен"
        };

        public string CleanerCleanCompleted => CurrentLanguage switch
        {
            "EN" => "Cleanup completed successfully!",
            "ZH" => "清理成功完成！",
            _ => "Очистка успешно завершена!"
        };

        public string CleanerFoundLabel => CurrentLanguage switch
        {
            "EN" => "Cache files found:",
            "ZH" => "发现缓存文件：",
            _ => "Найдено файлов кэша:"
        };

        public string CleanerFreedLabel => CurrentLanguage switch
        {
            "EN" => "Space freed up:",
            "ZH" => "已释放空间：",
            _ => "Освобождено пространства:"
        };

        // Startup
        public string StartupDesc => CurrentLanguage switch
        {
            "EN" => "Configure applications that run automatically when the system starts",
            "ZH" => "配置系统启动时自动运行的应用程序",
            _ => "Настройка автоматического запуска приложений при включении системы"
        };

        public string StartupRefresh => CurrentLanguage switch
        {
            "EN" => "Refresh List",
            "ZH" => "刷新列表",
            _ => "Обновить список"
        };

        public string StartupToggleText => CurrentLanguage switch
        {
            "EN" => "Run SystemHub when Windows starts",
            "ZH" => "开机时自动启动 SystemHub",
            _ => "Запускать SystemHub при входе в Windows"
        };

        public string StartupToggleSubtext => CurrentLanguage switch
        {
            "EN" => "The application will automatically start in the background when the operating system launches",
            "ZH" => "操作系统启动时，该应用程序将在后台自动运行",
            _ => "Приложение будет автоматически запускаться в фоновом режиме при запуске операционной системы"
        };

        public string StartupActiveTitle => CurrentLanguage switch
        {
            "EN" => "Active Startup Programs",
            "ZH" => "活动启动项",
            _ => "Активные элементы автозапуска"
        };

        public string StartupLocationUser => CurrentLanguage switch
        {
            "EN" => "User (HKCU)",
            "ZH" => "用户 (HKCU)",
            _ => "Пользовательский (HKCU)"
        };

        public string StartupLocationSystem => CurrentLanguage switch
        {
            "EN" => "System (HKLM)",
            "ZH" => "系统 (HKLM)",
            _ => "Системный (HKLM)"
        };

        public string StartupBtnDelete => CurrentLanguage switch
        {
            "EN" => "Remove",
            "ZH" => "删除",
            _ => "Удалить"
        };

        public string TrayMenuOpen => CurrentLanguage switch
        {
            "EN" => "Open",
            "ZH" => "打开",
            _ => "Открыть"
        };

        public string TrayMenuExit => CurrentLanguage switch
        {
            "EN" => "Exit",
            "ZH" => "退出",
            _ => "Выйти"
        };

        // About
        public string AboutHeader => CurrentLanguage switch
        {
            "EN" => "About Application",
            "ZH" => "关于应用",
            _ => "О приложении"
        };

        public string AboutTitle => CurrentLanguage switch
        {
            "EN" => "SystemHub Manager",
            "ZH" => "SystemHub 管理器",
            _ => "SystemHub Менеджер"
        };

        public string AboutVersion => CurrentLanguage switch
        {
            "EN" => "Version",
            "ZH" => "版本",
            _ => "Версия"
        };

        public string AboutDev => CurrentLanguage switch
        {
            "EN" => "Developer",
            "ZH" => "开发者",
            _ => "Разработчик"
        };

        public string AboutDesc => CurrentLanguage switch
        {
            "EN" => "A modern, premium system utility. Designed to manage system cleaning, monitor hardware specifications, check weather, configure startup applications, and control active media playback.",
            "ZH" => "一款现代高级系统实用工具。旨在管理系统清理、监控硬件规格、检查天气、配置启动应用程序以及控制活动媒体播放。",
            _ => "Современная премиальная системная утилита. Предназначена для очистки системы, мониторинга характеристик ПК, просмотра погоды, настройки автозапуска приложений и контроля воспроизведения медиа."
        };

        public string AboutLicense => CurrentLanguage switch
        {
            "EN" => "License",
            "ZH" => "许可证",
            _ => "Лицензия"
        };

        public string AboutLicenseType => CurrentLanguage switch
        {
            "EN" => "MIT License",
            "ZH" => "MIT 许可证",
            _ => "MIT Лицензия"
        };

        public string AboutFeaturesTitle => CurrentLanguage switch
        {
            "EN" => "Key Features",
            "ZH" => "核心功能",
            _ => "Ключевые возможности"
        };

        public string AboutFeatureWeatherTitle => CurrentLanguage switch
        {
            "EN" => "Weather Forecast",
            "ZH" => "天气预报",
            _ => "Прогноз погоды"
        };

        public string AboutFeatureWeatherDesc => CurrentLanguage switch
        {
            "EN" => "Accurate weather forecast for your city with automatic location detection.",
            "ZH" => "自动定位您所在城市并获取精准的天气预报。",
            _ => "Точный прогноз погоды для вашего города с автоматическим определением локации."
        };

        public string AboutFeatureSystemTitle => CurrentLanguage switch
        {
            "EN" => "About Computer",
            "ZH" => "关于电脑",
            _ => "О компьютере"
        };

        public string AboutFeatureSystemDesc => CurrentLanguage switch
        {
            "EN" => "Detailed information about your processor, graphics card, RAM, and disks.",
            "ZH" => "提供处理器、显ка、运行内存及硬盘的详细配置信息。",
            _ => "Детальная информация о процессоре, видеокарте, оперативной памяти и дисках."
        };

        public string AboutCheckUpdates => CurrentLanguage switch
        {
            "EN" => "Check for Updates",
            "ZH" => "检查更新",
            _ => "Проверить обновления"
        };

        public string AboutDownload => CurrentLanguage switch
        {
            "EN" => "Download",
            "ZH" => "下载",
            _ => "Скачать"
        };

        public string AboutFeatureCleanerTitle => CurrentLanguage switch
        {
            "EN" => "System Cleaner",
            "ZH" => "系统清理",
            _ => "Очистка кэша"
        };

        public string AboutFeatureCleanerDesc => CurrentLanguage switch
        {
            "EN" => "Safe removal of temporary files, logs and system cache.",
            "ZH" => "安全删除临时文件、日志和系统缓存。",
            _ => "Безопасное удаление временных файлов, логов и системного кэша."
        };

        public string AboutFeatureStartupTitle => CurrentLanguage switch
        {
            "EN" => "Startup Control",
            "ZH" => "启动项管理",
            _ => "Автозапуск"
        };

        public string AboutFeatureStartupDesc => CurrentLanguage switch
        {
            "EN" => "Toggle startup items to optimize system boot time.",
            "ZH" => "管理开机自启项以优化系统启动速度。",
            _ => "Настройка автозагрузки приложений для ускорения включения ПК."
        };

        public string AboutFeaturePlayerTitle => CurrentLanguage switch
        {
            "EN" => "Media Hub",
            "ZH" => "媒体中心",
            _ => "Медиаплеер"
        };

        public string AboutFeaturePlayerDesc => CurrentLanguage switch
        {
            "EN" => "Control all active sound and media playback sessions in one place.",
            "ZH" => "在统一界面控制所有活动的音频和媒体播放会话。",
            _ => "Управление всеми активными музыкальными и видеосессиями в одном месте."
        };

        public string AboutFeatureInstallerTitle => CurrentLanguage switch
        {
            "EN" => "App Installer",
            "ZH" => "软件安装",
            _ => "Установка программ"
        };

        public string AboutFeatureInstallerDesc => CurrentLanguage switch
        {
            "EN" => "Quick and secure installation of popular software using package manager.",
            "ZH" => "使用包管理器快速且安全地安装常用必备软件。",
            _ => "Быстрая и безопасная установка популярных программ с помощью пакетного менеджера."
        };

        public string AboutTechTitle => CurrentLanguage switch
        {
            "EN" => "Technology Stack",
            "ZH" => "技术栈",
            _ => "Технологический стек"
        };

        public string AboutTechDesc => CurrentLanguage switch
        {
            "EN" => "This modern desktop utility is built using C#, .NET 8.0, Avalonia UI, and CommunityToolkit.Mvvm libraries.",
            "ZH" => "这款现代桌面实用工具基于 C#、.NET 8.0、Avalonia UI 和 CommunityToolkit.Mvvm 开发。",
            _ => "Современное десктопное приложение, построенное на базе C#, .NET 8.0, Avalonia UI и библиотек CommunityToolkit.Mvvm."
        };

        // System view additional localizations
        public string SystemViewHeader => CurrentLanguage switch
        {
            "EN" => "About Computer",
            "ZH" => "关于电脑",
            _ => "О компьютере"
        };

        public string SystemViewSubheader => CurrentLanguage switch
        {
            "EN" => "Detailed hardware and operating system specifications",
            "ZH" => "详细的硬件和操作系统规格信息",
            _ => "Подробные технические характеристики компьютера и операционной системы"
        };

        public string SystemOsTitle => CurrentLanguage switch
        {
            "EN" => "Operating System",
            "ZH" => "操作系统",
            _ => "Операционная система"
        };

        public string SystemHardwareTitle => CurrentLanguage switch
        {
            "EN" => "Computer Specifications",
            "ZH" => "电脑规格参数",
            _ => "Характеристики компьютера"
        };

        public string SystemMotherboardLabel => CurrentLanguage switch
        {
            "EN" => "Motherboard:",
            "ZH" => "主板:",
            _ => "Материнская плата:"
        };

        public string SystemCpuLabel => CurrentLanguage switch
        {
            "EN" => "Processor (CPU):",
            "ZH" => "处理器 (CPU):",
            _ => "Процессор (CPU):"
        };

        public string SystemFrequencyLabel => CurrentLanguage switch
        {
            "EN" => "Frequency / Cores:",
            "ZH" => "频率 / 核心:",
            _ => "Частота / Ядра:"
        };

        public string SystemGpuLabel => CurrentLanguage switch
        {
            "EN" => "Graphics (GPU):",
            "ZH" => "显卡 (GPU):",
            _ => "Видеокарта (GPU):"
        };

        public string SystemRealtimePerf => CurrentLanguage switch
        {
            "EN" => "Real-Time Performance",
            "ZH" => "实时性能监控",
            _ => "Производительность в реальном времени"
        };

        public string SystemCpuLoad => CurrentLanguage switch
        {
            "EN" => "Processor Load (CPU)",
            "ZH" => "处理器负载 (CPU)",
            _ => "Загрузка процессора (CPU)"
        };

        public string SystemRamLabel => CurrentLanguage switch
        {
            "EN" => "Memory (RAM)",
            "ZH" => "内存 (RAM)",
            _ => "Память (RAM)"
        };

        public string SystemDrivesTitle => CurrentLanguage switch
        {
            "EN" => "Storage Drives",
            "ZH" => "磁盘驱动器",
            _ => "Дисковые накопители"
        };

        public string SystemFreeLabel => CurrentLanguage switch
        {
            "EN" => "free",
            "ZH" => "可用",
            _ => "свободно"
        };

        public string SystemTotalLabel => CurrentLanguage switch
        {
            "EN" => "of",
            "ZH" => "из",
            _ => "из"
        };

        public string SystemCoresLabel => CurrentLanguage switch
        {
            "EN" => "cores",
            "ZH" => "核心",
            _ => "ядер"
        };

        public string SystemThreadsLabel => CurrentLanguage switch
        {
            "EN" => "threads",
            "ZH" => "线程",
            _ => "потоков"
        };

        public string SystemVersionLabel => CurrentLanguage switch
        {
            "EN" => "Version:",
            "ZH" => "版本:",
            _ => "Версия:"
        };

        public string SystemBuildLabel => CurrentLanguage switch
        {
            "EN" => "OS Build:",
            "ZH" => "系统版本号:",
            _ => "Сборка ОС:"
        };

        public string SystemArchLabel => CurrentLanguage switch
        {
            "EN" => "Architecture:",
            "ZH" => "架构:",
            _ => "Архитектура:"
        };

        public string SystemDiskRootFolders => CurrentLanguage switch
        {
            "EN" => "Root folders of the disk:",
            "ZH" => "磁盘根文件夹:",
            _ => "Корневые папки диска:"
        };

        public string SystemDiskEmptyOrRestricted => CurrentLanguage switch
        {
            "EN" => "Folders are empty or restricted",
            "ZH" => "无文件夹或访问受限",
            _ => "Папки отсутствуют или недоступны"
        };

        public string SystemDiskAccessDenied => CurrentLanguage switch
        {
            "EN" => "Access denied",
            "ZH" => "拒绝访问",
            _ => "Доступ ограничен"
        };

        // Cleaner view additional localizations
        public string CleanerHeader => CurrentLanguage switch
        {
            "EN" => "Disk Cleaner",
            "ZH" => "磁盘清理",
            _ => "Очистка диска"
        };

        public string CleanerSubheader => CurrentLanguage switch
        {
            "EN" => "Safely remove temporary cache files and empty system Recycle Bin",
            "ZH" => "安全删除临时缓存文件并清空系统回收站",
            _ => "Безопасное удаление временных файлов кэша и очистка системной Корзины"
        };

        public string CleanerScanBtn => CurrentLanguage switch
        {
            "EN" => "Scan",
            "ZH" => "分析",
            _ => "Анализ"
        };

        public string CleanerScanningBtn => CurrentLanguage switch
        {
            "EN" => "Scanning...",
            "ZH" => "分析中...",
            _ => "Анализ..."
        };

        public string CleanerFoundFiles => CurrentLanguage switch
        {
            "EN" => "Temporary files found",
            "ZH" => "已发现的临时文件",
            _ => "Найдено временных файлов"
        };

        public string CleanerCategoriesHeader => CurrentLanguage switch
        {
            "EN" => "Section Analysis",
            "ZH" => "分区分析",
            _ => "Анализ разделов"
        };

        public string CleanerUserTempTitle => CurrentLanguage switch
        {
            "EN" => "User Temp Files",
            "ZH" => "用户临时文件",
            _ => "Временные файлы пользователя"
        };

        public string CleanerUserTempDesc => CurrentLanguage switch
        {
            "EN" => "User application cache and diagnostic logs",
            "ZH" => "用户应用缓存和运行日志",
            _ => "Пользовательский кэш и логи приложений"
        };

        public string CleanerSystemTempTitle => CurrentLanguage switch
        {
            "EN" => "System Temp Files",
            "ZH" => "系统临时文件",
            _ => "Временные файлы системы"
        };

        public string CleanerSystemTempDesc => CurrentLanguage switch
        {
            "EN" => "Windows update cache and crash logs",
            "ZH" => "系统更新缓存和错误转储文件",
            _ => "Кэш обновлений и системные дампы ошибок"
        };

        public string CleanerPrefetchTitle => CurrentLanguage switch
        {
            "EN" => "Prefetch Cache",
            "ZH" => "Prefetch 预取缓存",
            _ => "Системный кэш Prefetch"
        };

        public string CleanerPrefetchDesc => CurrentLanguage switch
        {
            "EN" => "Windows boot optimization cache files",
            "ZH" => "Windows 启动加速缓存文件",
            _ => "Предварительно загружаемые кэши запуска Windows"
        };

        public string CleanerRecycleBinTitle => CurrentLanguage switch
        {
            "EN" => "System Recycle Bin",
            "ZH" => "系统回收站",
            _ => "Корзина системы"
        };

        public string CleanerRecycleBinDesc => CurrentLanguage switch
        {
            "EN" => "Deleted files from all storage drives",
            "ZH" => "所有磁盘中已删除到回收站的文件",
            _ => "Файлы, удаленные в Корзину со всех дисков"
        };

        // Weather view additional localizations
        public string WeatherHeader => CurrentLanguage switch
        {
            "EN" => "Weather",
            "ZH" => "天气",
            _ => "Погода"
        };

        public string WeatherWindUnit => CurrentLanguage switch
        {
            "EN" => "km/h",
            "ZH" => "千米/时",
            _ => "км/ч"
        };

        public string WeatherForecastHeader => CurrentLanguage switch
        {
            "EN" => "7-Day Forecast",
            "ZH" => "7 天天气预报",
            _ => "Прогноз на 7 дней"
        };

        public string TranslateWeatherCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return "";
            
            // Clean up suffix for night
            bool isNight = condition.EndsWith(" (ночь)") || condition.EndsWith(" (night)") || condition.EndsWith(" (夜间)");
            string baseCond = condition.Replace(" (ночь)", "").Replace(" (night)", "").Replace(" (夜间)", "").Trim();

            string translated = baseCond switch
            {
                "Ясно" or "Clear" => CurrentLanguage switch
                {
                    "EN" => "Clear",
                    "ZH" => "晴",
                    _ => "Ясно"
                },
                "Переменная облачность" or "Partly Cloudy" => CurrentLanguage switch
                {
                    "EN" => "Partly Cloudy",
                    "ZH" => "多云",
                    _ => "Переменная облачность"
                },
                "Пасмурно" or "Overcast" => CurrentLanguage switch
                {
                    "EN" => "Overcast",
                    "ZH" => "阴",
                    _ => "Пасмурно"
                },
                "Туман" or "Foggy" or "Fog" => CurrentLanguage switch
                {
                    "EN" => "Foggy",
                    "ZH" => "雾",
                    _ => "Туман"
                },
                "Морось" or "Drizzle" => CurrentLanguage switch
                {
                    "EN" => "Drizzle",
                    "ZH" => "毛毛雨",
                    _ => "Морось"
                },
                "Дождь" or "Rainy" or "Rain" => CurrentLanguage switch
                {
                    "EN" => "Rainy",
                    "ZH" => "雨",
                    _ => "Дождь"
                },
                "Снегопад" or "Snowy" or "Snow" => CurrentLanguage switch
                {
                    "EN" => "Snowy",
                    "ZH" => "雪",
                    _ => "Снегопад"
                },
                "Ливень" or "Heavy Rain" => CurrentLanguage switch
                {
                    "EN" => "Heavy Rain",
                    "ZH" => "暴雨",
                    _ => "Ливень"
                },
                "Гроза" or "Thunderstorm" => CurrentLanguage switch
                {
                    "EN" => "Thunderstorm",
                    "ZH" => "雷阵雨",
                    _ => "Гроза"
                },
                _ => CurrentLanguage switch
                {
                    "EN" => "Unknown",
                    "ZH" => "未知",
                    _ => condition
                }
            };

            if (isNight)
            {
                translated += CurrentLanguage switch
                {
                    "EN" => " (night)",
                    "ZH" => " (夜间)",
                    _ => " (ночь)"
                };
            }

            return translated;
        }

        public string TranslateDayName(string dayName)
        {
            if (string.IsNullOrEmpty(dayName)) return "";

            if (dayName == "Сегодня" || dayName == "Today" || dayName == "今天")
            {
                return CurrentLanguage switch
                {
                    "EN" => "Today",
                    "ZH" => "今天",
                    _ => "Сегодня"
                };
            }

            // Weekday translation
            string cleanDay = dayName.ToLower().Replace(".", "").Trim();
            
            if (cleanDay == "пн" || cleanDay == "mon" || cleanDay == "monday")
                return CurrentLanguage switch { "EN" => "Mon", "ZH" => "周一", _ => "Пн" };
            if (cleanDay == "вт" || cleanDay == "tue" || cleanDay == "tuesday")
                return CurrentLanguage switch { "EN" => "Tue", "ZH" => "周二", _ => "Вт" };
            if (cleanDay == "ср" || cleanDay == "wed" || cleanDay == "wednesday")
                return CurrentLanguage switch { "EN" => "Wed", "ZH" => "周三", _ => "Ср" };
            if (cleanDay == "чт" || cleanDay == "thu" || cleanDay == "thursday")
                return CurrentLanguage switch { "EN" => "Thu", "ZH" => "周四", _ => "Чт" };
            if (cleanDay == "пт" || cleanDay == "fri" || cleanDay == "friday")
                return CurrentLanguage switch { "EN" => "Fri", "ZH" => "周五", _ => "Пт" };
            if (cleanDay == "сб" || cleanDay == "sat" || cleanDay == "saturday")
                return CurrentLanguage switch { "EN" => "Sat", "ZH" => "周六", _ => "Сб" };
            if (cleanDay == "вс" || cleanDay == "sun" || cleanDay == "sunday")
                return CurrentLanguage switch { "EN" => "Sun", "ZH" => "周日", _ => "Вс" };

            return dayName;
        }
        public string WeatherSearchWatermark => CurrentLanguage switch
        {
            "EN" => "If the city is not yours, write its name here...",
            "ZH" => "如果这不是您的城市，请在此输入它的名称...",
            _ => "Если город не ваш, напишите его название сюда..."
        };

        public string WeatherSearchBtn => CurrentLanguage switch
        {
            "EN" => "Find",
            "ZH" => "搜索",
            _ => "Найти"
        };

        public string WeatherResetLocation => CurrentLanguage switch
        {
            "EN" => "My Location",
            "ZH" => "我的位置",
            _ => "Моя локация"
        };

        public string WeatherLocationSettingsHeader => CurrentLanguage switch
        {
            "EN" => "Location Settings",
            "ZH" => "位置设置",
            _ => "Настройка местоположения"
        };

        public string WeatherLocationSettingsDesc => CurrentLanguage switch
        {
            "EN" => "Search for any city or town to view its weather forecast",
            "ZH" => "搜索任何城市或城镇以查看天气预报",
            _ => "Введите название города или поселка для поиска погоды"
        };

        public string WeatherLocationDialogTitle => CurrentLanguage switch
        {
            "EN" => "Geolocation Setup",
            "ZH" => "地理位置设置",
            _ => "Настройка геопозиции"
        };

        public string WeatherLocationDialogDesc => CurrentLanguage switch
        {
            "EN" => "1. Choose your location in the opened Yandex Maps.\n2. Copy the URL from address bar (or right-click the map and copy coordinates).\n3. Paste the copied link/coordinates below:",
            "ZH" => "1. 在打开的 Yandex 地图上选择您所在的位置。\n2. 从地址栏复制网址 (或右键点击地图并复制坐标)。\n3. 在下方粘贴所复制의链接/坐标：",
            _ => "1. В открывшихся Яндекс.Картах выберите ваше местоположение.\n2. Скопируйте ссылку из адресной строки (или нажмите правой кнопкой на карту и скопируйте координаты).\n3. Вставьте скопированную ссылку или координаты ниже:"
        };

        public string WeatherLocationDialogSuccess => CurrentLanguage switch
        {
            "EN" => "Link/coordinates recognized!",
            "ZH" => "链接/坐标已识别！",
            _ => "Ссылка/координаты распознаны!"
        };

        public string WeatherLocationDialogCancel => CurrentLanguage switch
        {
            "EN" => "Cancel",
            "ZH" => "Отмена",
            _ => "Отмена"
        };

        public string WeatherSearchResultsTitle => CurrentLanguage switch
        {
            "EN" => "Search Results",
            "ZH" => "搜索结果",
            _ => "Результаты поиска"
        };


        public string WeatherCoordinatesWatermark => CurrentLanguage switch
        {
            "EN" => "Paste coordinates (54.92, 43.34) or Google/Yandex/2GIS link...",
            "ZH" => "粘贴坐标 (54.92, 43.34) 或 Google/Yandex/2GIS 链接...",
            _ => "Вставьте координаты (54.92, 43.34) или ссылку Google/Яндекс/2ГИС..."
        };

        public string WeatherCoordinatesBtn => CurrentLanguage switch
        {
            "EN" => "Apply",
            "ZH" => "应用",
            _ => "Применить"
        };

        public string WeatherCoordinatesError => CurrentLanguage switch
        {
            "EN" => "Could not parse coordinates or map link",
            "ZH" => "无法解析坐标或地图链接",
            _ => "Не удалось распознать координаты или ссылку"
        };

        public string VolumeMixerHeader => CurrentLanguage switch
        {
            "EN" => "App Volume Mixer",
            "ZH" => "应用音量 mixer",
            _ => "Микшер громкости программ"
        };

        public string VolumeMixerNoApps => CurrentLanguage switch
        {
            "EN" => "No active applications with audio",
            "ZH" => "未检测到活动音频应用",
            _ => "Нет активных приложений со звуком"
        };

        public string SidebarInstaller => CurrentLanguage switch
        {
            "EN" => "App Installer",
            "ZH" => "软件安装",
            _ => "Установка программ"
        };

        public string InstallerHeader => CurrentLanguage switch
        {
            "EN" => "Software Installer",
            "ZH" => "必备软件安装器",
            _ => "Установка программ"
        };

        public string InstallerDesc => CurrentLanguage switch
        {
            "EN" => "Quick and secure installation of popular software using Windows Package Manager (winget).",
            "ZH" => "通过 Windows 官方包管理器 (winget) 安全且极速地安装常用装机必备软件。",
            _ => "Быстрая и безопасная установка популярных программ через официальный пакетный менеджер Windows (winget)."
        };

        public string InstallerStatusNotInstalled => CurrentLanguage switch
        {
            "EN" => "Not Installed",
            "ZH" => "未安装",
            _ => "Не установлено"
        };

        public string InstallerStatusQueued => CurrentLanguage switch
        {
            "EN" => "In Queue",
            "ZH" => "队列中",
            _ => "В очереди"
        };

        public string InstallerStatusInstalling => CurrentLanguage switch
        {
            "EN" => "Installing...",
            "ZH" => "正在安装...",
            _ => "Установка..."
        };

        public string InstallerStatusInstalled => CurrentLanguage switch
        {
            "EN" => "Installed",
            "ZH" => "已安装",
            _ => "Установлено"
        };

        public string InstallerStatusFailed => CurrentLanguage switch
        {
            "EN" => "Failed",
            "ZH" => "安装失败",
            _ => "Ошибка"
        };

        public string InstallerBtnInstall => CurrentLanguage switch
        {
            "EN" => "Install",
            "ZH" => "安装",
            _ => "Установить"
        };

        public string InstallerBtnInstallSelected => CurrentLanguage switch
        {
            "EN" => "Install Selected",
            "ZH" => "安装所选",
            _ => "Установить выбранные"
        };

        public string InstallerWidgetTitle => CurrentLanguage switch
        {
            "EN" => "Quick Installer",
            "ZH" => "快捷装机",
            _ => "Быстрая установка"
        };

        public string InstallerWidgetOpenLink => CurrentLanguage switch
        {
            "EN" => "Open full list",
            "ZH" => "查看完整列表",
            _ => "Открыть полный список"
        };

        public string InstallerBtnScan => CurrentLanguage switch
        {
            "EN" => "Scan Installed",
            "ZH" => "扫描已安装",
            _ => "Сканировать"
        };

        public string InstallerStatusScanning => CurrentLanguage switch
        {
            "EN" => "Scanning...",
            "ZH" => "正在扫描...",
            _ => "Сканирование..."
        };

        // Versions selection
        public string VersionLabel => CurrentLanguage switch
        {
            "EN" => "Version:",
            "ZH" => "版本:",
            _ => "Версия:"
        };

        public string VersionSelectorModalTitle => CurrentLanguage switch
        {
            "EN" => "Choose what you will download",
            "ZH" => "选择您要下载的版本",
            _ => "Выберите, что будете скачивать"
        };

        public string VersionRegular => CurrentLanguage switch
        {
            "EN" => "Regular",
            "ZH" => "原版",
            _ => "Обычная"
        };

        public string VersionMod => CurrentLanguage switch
        {
            "EN" => "Mod",
            "ZH" => "修改版",
            _ => "Мод"
        };

        public string VersionOfficial => CurrentLanguage switch
        {
            "EN" => "Official",
            "ZH" => "官方版",
            _ => "Официальный"
        };

        public string VersionAyuGram => CurrentLanguage switch
        {
            "EN" => "AyuGram",
            "ZH" => "AyuGram",
            _ => "AyuGram"
        };

        public string VersionSpotX => CurrentLanguage switch
        {
            "EN" => "SpotX",
            "ZH" => "SpotX",
            _ => "SpotX"
        };

        // Spotify versions descriptions
        public string SpotifyModName => CurrentLanguage switch
        {
            "EN" => "Spotify (SpotX)",
            "ZH" => "Spotify (SpotX 修改版)",
            _ => "Spotify (SpotX)"
        };

        public string DescSpotifyMod => CurrentLanguage switch
        {
            "EN" => "Modified Spotify client for PC without ads and with extra features.",
            "ZH" => "无广告且包含额外功能的 Spotify (SpotX) 桌面修改版。",
            _ => "Модифицированная версия Spotify для ПК без рекламы и с дополнительными функциями."
        };

        // Yandex Music versions descriptions
        public string YandexMusicName => CurrentLanguage switch
        {
            "EN" => "Yandex Music",
            "ZH" => "Yandex Music",
            _ => "Яндекс Музыка"
        };

        public string DescYandexMusic => CurrentLanguage switch
        {
            "EN" => "Yandex Music streaming service for Windows.",
            "ZH" => "Windows 平台 Yandex Music 流媒体服务客户端。",
            _ => "Стриминговый сервис Яндекс Музыка для Windows."
        };

        // Telegram versions descriptions
        public string TelegramModName => CurrentLanguage switch
        {
            "EN" => "AyuGram Desktop",
            "ZH" => "AyuGram 桌面版",
            _ => "AyuGram Desktop"
        };

        public string DescTelegramMod => CurrentLanguage switch
        {
            "EN" => "Modified Telegram client with anti-delete messages, ghost mode, and other improvements.",
            "ZH" => "支持防撤回、隐身模式等功能的高级 Telegram 修改版客户端。",
            _ => "Модифицированный клиент Telegram с защитой от удаления сообщений, скрытым режимом и другими улучшениями."
        };

        public string YandexMusicModName => CurrentLanguage switch
        {
            "EN" => "Yandex Music Mod (Beta)",
            "ZH" => "Yandex Music 修改版",
            _ => "Яндекс Музыка (Mod)"
        };

        public string YandexMusicModDesc => CurrentLanguage switch
        {
            "EN" => "Modified version of Yandex Music without ads and limitations.",
            "ZH" => "去广告且无限制的 Yandex 音乐修改版。",
            _ => "Модифицированная версия Яндекс Музыки без рекламы и ограничений."
        };

        public string ZapretName => CurrentLanguage switch
        {
            "EN" => "Zapret (YouTube/Discord)",
            "ZH" => "Zapret (YouTube/Discord 绕过)",
            _ => "Zapret (YouTube/Discord)"
        };

        public string ZapretDesc => CurrentLanguage switch
        {
            "EN" => "Bypass YouTube and Discord restrictions/blocking in Russia.",
            "ZH" => "绕过俄罗斯对 YouTube 和 Discord 的封锁与限制。",
            _ => "Обход ограничений и замедления YouTube и Discord в России."
        };

        public string ZapretUnlockFaceit => CurrentLanguage switch
        {
            "EN" => "Unlock Faceit Servers",
            "ZH" => "解锁 Faceit 服务器",
            _ => "Разблокировать зарубежные сервера"
        };

        public string ZapretLockFaceit => CurrentLanguage switch
        {
            "EN" => "Lock Faceit Servers",
            "ZH" => "锁定 Faceit 服务器",
            _ => "Заблокировать Faceit сервера"
        };


        public string ZapretUpdateBtn => CurrentLanguage switch
        {
            "EN" => "Update",
            "ZH" => "更新",
            _ => "Обновить"
        };

        public string ZapretModalTitle => CurrentLanguage switch
        {
            "EN" => "Zapret Installation Guide",
            "ZH" => "Zapret 安装向导",
            _ => "Инструкция по установке Zapret"
        };

        public string ZapretModalDesc => CurrentLanguage switch
        {
            "EN" => "To bypass YouTube and Discord blocking, we will now start the installation of the zapret service.\n\nAfter unpacking, a black Administrator command line window titled «ZAPRET SERVICE MANAGER v1.9.9a» will open.\n\nIn that open console window:\n1. Type 1 and press Enter (to install the service)\n2. Type 12 and press Enter (to exit the menu)\n\nClick 'Install' to start downloading and running the installation script.",
            "ZH" => "为了绕过对 YouTube 和 Discord 的封锁，我们将开始安装 zapret 服务。\n\n解压完成后，会打开一个名为“ZAPRET SERVICE MANAGER v1.9.9a”的管理员命令行窗口。\n\n在打开的控制台窗口中：\n1. 输入 1 并按 Enter（以安装服务）\n2. 输入 12 并按 Enter（以退出菜单）\n\n点击“安装”以开始下载并运行安装脚本。",
            _ => "Для обхода блокировок YouTube и Discord сейчас начнется установка службы zapret.\n\nПосле распаковки откроется черное окно командной строки администратора «ZAPRET SERVICE MANAGER v1.9.9a».\n\nВ открывшемся консольном окне вам нужно:\n1. Написать цифру 1 и нажать Enter (для установки службы)\n2. Написать цифру 12 и нажать Enter (для выхода)\n\nНажмите «Установить» для скачивания и запуска скрипта установки."
        };

        public string SpotXModalInstructions => CurrentLanguage switch
        {
            "EN" => "To successfully configure SpotX:\n1. Click «Install» below to start the download.\n2. Wait for PowerShell to launch (a blue/black console window will open).\n3. Spotify will download and install inside the opened console window (this might take a moment).\n4. When prompted «Hide podcasts, shows and audiobooks on the main page? [Y/N]»:\n5. Type «y» and press Enter.\n6. Wait for the installer to finish.",
            "ZH" => "成功配置 SpotX 的步骤：\n1. 点击下方的“安装”开始下载。\n2. 等待 PowerShell 启动（将打开一个蓝色/黑色的控制台窗口）。\n3. Spotify 本身将在打开的控制台窗口内进行下载并安装（这可能需要一些时间）。\n4. 当提示 «Hide podcasts, shows and audiobooks on the main page? [Y/N]»（是否在主页隐藏播客、节目和有声书？）时：\n5. 输入 «y» 并按回车键。\n6. 等待安装程序运行完毕。",
            _ => "Для успешной настройки SpotX:\n1. Нажмите «Установить» ниже, чтобы начать загрузку.\n2. Дождитесь запуска PowerShell (откроется синее/черное консольное окно).\n3. Начнется скачивание и установка Spotify внутри открывшегося окна (это может занять некоторое время).\n4. В консоли появится вопрос «Hide podcasts, shows and audiobooks on the main page? [Y/N]» (Скрыть подкасты, шоу и аудиокниги на главной странице?).\n5. Напишите «y» на клавиатуре и нажмите Enter.\n6. Дождитесь завершения работы установщика."
        };

        public string SpotXModalInstructionsHeader => CurrentLanguage switch
        {
            "EN" => "⚠️ Installation Instructions:",
            "ZH" => "⚠️ 安装向导：",
            _ => "⚠️ Инструкция по установке:"
        };

        // Media player launcher localizations
        public string PlayerOpenSpotify => CurrentLanguage switch
        {
            "EN" => "Open Spotify",
            "ZH" => "打开 Spotify",
            _ => "Открыть Spotify"
        };

        public string PlayerOpenYandex => CurrentLanguage switch
        {
            "EN" => "Open Yandex Music",
            "ZH" => "打开 Yandex 音乐",
            _ => "Открыть Яндекс.Музыку"
        };

        public string PlayerOpenApp => CurrentLanguage switch
        {
            "EN" => "Desktop App",
            "ZH" => "桌面应用",
            _ => "Приложение"
        };

        public string PlayerOpenWeb => CurrentLanguage switch
        {
            "EN" => "Web Version",
            "ZH" => "网页版",
            _ => "Веб-версия"
        };

        public string PlayerSessionsToggle => CurrentLanguage switch
        {
            "EN" => "Show/Hide Active Sessions",
            "ZH" => "显示/隐藏活动会话",
            _ => "Свернуть/развернуть активные сессии"
        };

        // Installer Category translations
        public string CategoryBrowsers => CurrentLanguage switch { "EN" => "Browsers", "ZH" => "浏览器", _ => "Браузеры" };
        public string CategoryMessengers => CurrentLanguage switch { "EN" => "Messengers", "ZH" => "即时通讯", _ => "Мессенджеры" };
        public string CategoryGames => CurrentLanguage switch { "EN" => "Games", "ZH" => "游戏", _ => "Игры" };
        public string CategoryPlayers => CurrentLanguage switch { "EN" => "Players", "ZH" => "播放器", _ => "Плееры" };
        public string CategoryUtilities => CurrentLanguage switch { "EN" => "Utilities", "ZH" => "实用工具", _ => "Утилиты" };

        // Program Descriptions
        public string DescChrome => CurrentLanguage switch 
        { 
            "EN" => "Fast, secure, and popular web browser by Google.", 
            "ZH" => "谷歌公司出品的快速、安全且流行的网页浏览器。", 
            _ => "Быстрый, безопасный и популярный веб-браузер от компании Google." 
        };
        public string DescFirefox => CurrentLanguage switch 
        { 
            "EN" => "Web browser by Mozilla. Fast, private and independent.", 
            "ZH" => "火狐浏览器。快速、私密且独立。", 
            _ => "Веб-браузер от Mozilla. Быстрый, приватный и независимый." 
        };
        public string DescYandexBrowser => CurrentLanguage switch 
        { 
            "EN" => "Fast and secure browser with voice assistant Alice and translation features.", 
            "ZH" => "快速安全的浏览器，配备语音助手 Alice 和翻译功能。", 
            _ => "Быстрый и безопасный браузер с голосовым помощником Алисой и переводчиком." 
        };
        public string DescDiscord => CurrentLanguage switch 
        { 
            "EN" => "Voice, video, and text communication service for gamers and creators.", 
            "ZH" => "面向玩家和创作者的语音、视频及文字聊天工具。", 
            _ => "Голосовой, видео- и текстовый чат для геймеров и создателей контента." 
        };
        public string DescSteam => CurrentLanguage switch 
        { 
            "EN" => "Popular gaming platform to play, discuss, and create games.", 
            "ZH" => "用于玩游戏、讨论和创作的流行游戏平台。", 
            _ => "Популярная игровая платформа для запуска игр, общения и творчества." 
        };
        public string DescVlc => CurrentLanguage switch 
        { 
            "EN" => "Free and open-source cross-platform multimedia player that plays most files.", 
            "ZH" => "支持播放大多数格式文件的免费开源跨平台多媒体播放器。", 
            _ => "Бесплатный медиаплеер с открытым исходным кодом, воспроизводящий большинство форматов." 
        };
        public string DescTelegram => CurrentLanguage switch 
        { 
            "EN" => "Fast and secure desktop messaging app with cloud synchronization.", 
            "ZH" => "具有云同步功能的高速、安全的桌面即时通讯软件。", 
            _ => "Быстрый и безопасный мессенджер с облачной синхронизацией сообщений." 
        };
        public string DescSpotify => CurrentLanguage switch 
        { 
            "EN" => "Streaming service providing access to millions of music tracks.", 
            "ZH" => "提供数百万首音乐曲目的流媒体服务。", 
            _ => "Стриминговый сервис, предоставляющий доступ к миллионам музыкальных треков." 
        };
        public string Desc7Zip => CurrentLanguage switch 
        { 
            "EN" => "Popular file archiver with a high compression ratio and AES-256 encryption.", 
            "ZH" => "具有高压缩比和 AES-256 加密功能的热门文件归档管理器。", 
            _ => "Популярный архиватор с высокой степенью сжатия файлов и шифрованием AES-256." 
        };

        // Tooltips
        public string ToolTipPrev => CurrentLanguage switch { "EN" => "Previous", "ZH" => "上一首", _ => "Назад" };
        public string ToolTipNext => CurrentLanguage switch { "EN" => "Next", "ZH" => "下一首", _ => "Вперед" };
        public string ToolTipVolume => CurrentLanguage switch { "EN" => "Volume", "ZH" => "音量", _ => "Громкость" };
        public string ToolTipCollapse => CurrentLanguage switch { "EN" => "Collapse", "ZH" => "折叠", _ => "Свернуть" };
        public string ToolTipClose => CurrentLanguage switch { "EN" => "Close", "ZH" => "关闭", _ => "Закрыть" };
        public string ToolTipMinimize => CurrentLanguage switch { "EN" => "Minimize", "ZH" => "最小化", _ => "Свернуть" };
        public string ToolTipMaximize => CurrentLanguage switch { "EN" => "Maximize", "ZH" => "最大化", _ => "Развернуть" };
        public string ToolTipPlayPause => CurrentLanguage switch { "EN" => "Play/Pause", "ZH" => "播放/暂停", _ => "Воспроизведение/Пауза" };

        // Dynamic Island
        public string DynamicIslandToggleLabel => CurrentLanguage switch { "EN" => "Enable Dynamic Island for media", "ZH" => "启用媒体灵动岛", _ => "Включить Dynamic Island для медиа" };
        public string DynamicIslandSettingsHeader => CurrentLanguage switch { "EN" => "Dynamic Island Settings", "ZH" => "灵动岛设置", _ => "Настройки Dynamic Island" };

        // Tweaks Page
        public string TweaksTitle => CurrentLanguage switch { "EN" => "System Customization", "ZH" => "系统个性化", _ => "Кастомизация системы" };
        public string TweakClassicMenu => CurrentLanguage switch { "EN" => "Classic Context Menu 🛡️", "ZH" => "经典上下文菜单 🛡️", _ => "Классическое контекстное меню 🛡️" };
        public string TweakClassicMenuDesc => CurrentLanguage switch { "EN" => "Restores the full legacy Windows 10/11 context menu. Requires restarting Explorer.", "ZH" => "恢复完整的经典右键菜单。需要重启资源管理器。", _ => "Возвращает контекстное меню Windows 10 в Windows 11. Требуется перезапуск Проводника." };
        public string TweakTranslucentTaskbar => CurrentLanguage switch { "EN" => "Translucent Taskbar", "ZH" => "透明任务栏", _ => "Прозрачная панель задач" };
        public string TweakTranslucentTaskbarDesc => CurrentLanguage switch { "EN" => "Makes the taskbar translucent with blur effect.", "ZH" => "使任务栏半透明并带有模糊效果。", _ => "Делает панель задач прозрачной с эффектом размытия." };
        public string TweakRemoveArrows => CurrentLanguage switch { "EN" => "Remove Shortcut Arrows 🛡️", "ZH" => "移除快捷方式箭头 🛡️", _ => "Убрать стрелочки ярлыков 🛡️" };
        public string TweakRemoveArrowsDesc => CurrentLanguage switch { "EN" => "Removes the shortcut arrow overlay from desktop icons.", "ZH" => "移除桌面图标上的快捷方式小箭头。", _ => "Убирает стрелки на значках ярлыков на рабочем столе." };
        public string TweakDisableUpdates => CurrentLanguage switch { "EN" => "Disable Auto Updates 🛡️", "ZH" => "禁用自动更新 🛡️", _ => "Отключить автообновления 🛡️" };
        public string TweakDisableUpdatesDesc => CurrentLanguage switch { "EN" => "Forcefully stops and disables the Windows Update service.", "ZH" => "强制停止并禁用 Windows Update 服务。", _ => "Принудительно останавливает и отключает службу автоматического обновления Windows." };
        public string TweakSystemFont => CurrentLanguage switch { "EN" => "System Font 🛡️", "ZH" => "系统字体 🛡️", _ => "Системный шрифт 🛡️" };
        public string TweakSystemFontDesc => CurrentLanguage switch { "EN" => "Changes the default system font to a modern custom font.", "ZH" => "将默认系统字体更改为现代自定义字体。", _ => "Меняет системный шрифт окон и проводника на выбранный современный шрифт." };
        public string TweakRestartExplorer => CurrentLanguage switch { "EN" => "Restart Explorer", "ZH" => "重启资源管理器", _ => "Перезапустить Проводник" };
        public string TweakRestartExplorerSuccess => CurrentLanguage switch { "EN" => "Explorer restarted successfully!", "ZH" => "资源管理器已成功重启！", _ => "Проводник успешно перезапущен!" };
        public string TweakApply => CurrentLanguage switch { "EN" => "Apply", "ZH" => "应用", _ => "Применить" };
        public string TweakApplied => CurrentLanguage switch { "EN" => "Applied", "ZH" => "Применено", _ => "Применено" };
        public string TweakRestore => CurrentLanguage switch { "EN" => "Restore", "ZH" => "恢复", _ => "Восстановить" };

        // Tools Page
        public string ToolsTitle => CurrentLanguage switch { "EN" => "Tools & Widgets", "ZH" => "工具与组件", _ => "Инструменты и Виджеты" };
        public string ToolFocusTimer => CurrentLanguage switch { "EN" => "Focus Timer (Pomodoro)", "ZH" => "专注番茄钟", _ => "Таймер фокуса (Pomodoro)" };
        public string FocusTimerStart => CurrentLanguage switch { "EN" => "Start", "ZH" => "开始", _ => "Старт" };
        public string FocusTimerPause => CurrentLanguage switch { "EN" => "Pause", "ZH" => "暂停", _ => "Пауза" };
        public string FocusTimerReset => CurrentLanguage switch { "EN" => "Reset", "ZH" => "重置", _ => "Сброс" };
        public string FocusTimerWork => CurrentLanguage switch { "EN" => "Work Session", "ZH" => "工作时间", _ => "Время работы" };
        public string FocusTimerBreak => CurrentLanguage switch { "EN" => "Break Session", "ZH" => "休息时间", _ => "Время отдыха" };
        
        public string ToolTempMail => CurrentLanguage switch { "EN" => "Temp Mail Widget", "ZH" => "临时邮箱组件", _ => "Временная почта" };
        public string TempMailGenerate => CurrentLanguage switch { "EN" => "Generate Inbox", "ZH" => "生成邮箱", _ => "Создать ящик" };
        public string TempMailCopied => CurrentLanguage switch { "EN" => "Address copied to clipboard!", "ZH" => "邮箱地址已复制到剪贴板！", _ => "Адрес скопирован в буфер обмена!" };
        public string TempMailNoMessages => CurrentLanguage switch { "EN" => "No messages received yet.", "ZH" => "暂无收到邮件。", _ => "Сообщений пока нет." };
        
        public string ToolFileShredder => CurrentLanguage switch { "EN" => "Secure File Shredder", "ZH" => "安全文件粉碎机", _ => "Шредер файлов" };
        public string FileShredderSelect => CurrentLanguage switch { "EN" => "Select Files...", "ZH" => "选择文件...", _ => "Выбрать файлы..." };
        public string FileShredderDrag => CurrentLanguage switch { "EN" => "Drag and drop files here", "ZH" => "将文件拖放到此处", _ => "Перетащите файлы сюда" };
        public string FileShredderWarning => CurrentLanguage switch { "EN" => "Warning! Selected files will be permanently overwritten and deleted. This action CANNOT be undone. Proceed?", "ZH" => "警告！所选文件将被永久覆写并删除。此操作无法撤销。是否继续？", _ => "Внимание! Выбранные файлы будут навсегда перезаписаны и удалены. Это действие НЕЛЬЗЯ отменить. Продолжить?" };
        public string FileShredderSuccess => CurrentLanguage switch { "EN" => "Files shredded successfully!", "ZH" => "文件粉碎成功！", _ => "Файлы успешно стерты!" };
        

        public string ToolToDo => CurrentLanguage switch { "EN" => "To-Do List (macOS Style)", "ZH" => "待办清单", _ => "Виджет Задач (To-Do)" };
        public string ToDoPlaceholder => CurrentLanguage switch { "EN" => "Add a new task...", "ZH" => "添加新任务...", _ => "Добавить задачу..." };
        
        public string ToolQrGenerator => CurrentLanguage switch { "EN" => "QR Code Generator", "ZH" => "二维码生成器", _ => "Генератор QR-кодов" };
        public string QrPlaceholder => CurrentLanguage switch { "EN" => "Enter URL or text to encode...", "ZH" => "输入要编码的网址或文本...", _ => "Введите текст или ссылку для QR..." };
        public string QrGenerate => CurrentLanguage switch { "EN" => "Generate QR", "ZH" => "生成二维码", _ => "Создать QR" };
        
        public string ToolDateCalc => CurrentLanguage switch { "EN" => "Date Calculator", "ZH" => "日期计算器", _ => "Калькулятор дат" };
        public string DateCalcDiff => CurrentLanguage switch { "EN" => "Difference in Days", "ZH" => "天数差", _ => "Разница в днях" };
        public string ToolWorldTime => CurrentLanguage switch { "EN" => "World Clocks", "ZH" => "世界时钟", _ => "Мировое время" };

        public string TempMailSender => CurrentLanguage switch { "EN" => "From", "ZH" => "发件人", _ => "От" };
        public string TempMailSubject => CurrentLanguage switch { "EN" => "Subject", "ZH" => "主题", _ => "Тема" };

        public string HardwareSmartHealth => CurrentLanguage switch { "EN" => "Disk SMART Health", "ZH" => "硬盘健康度", _ => "Здоровье дисков S.M.A.R.T." };
        public string HardwareTemperatures => CurrentLanguage switch { "EN" => "Temperatures", "ZH" => "温度监控", _ => "Температуры" };
        public string HardwareCpuTemp => CurrentLanguage switch { "EN" => "CPU Temperature", "ZH" => "CPU 温度", _ => "Температура ЦП" };
        public string HardwareGpuTemp => CurrentLanguage switch { "EN" => "GPU Temperature", "ZH" => "GPU 温度", _ => "Температура ГП" };
        public string HardwareDiskHealthNormal => CurrentLanguage switch { "EN" => "Good (Healthy)", "ZH" => "良好 (健康)", _ => "Хорошее (Здоров)" };
        public string HardwareDiskHealthWarning => CurrentLanguage switch { "EN" => "Warning (Degraded)", "ZH" => "警告 (有磨损)", _ => "Внимание (Есть износ)" };
        public string HardwareDiskHealthCritical => CurrentLanguage switch { "EN" => "Critical! Replace SSD/HDD!", "ZH" => "严重！请更换硬盘！", _ => "Критическое! Замените диск!" };

        // Weather Widget
        public string WeatherWidgetRefresh => CurrentLanguage switch { "EN" => "Refresh", "ZH" => "刷新", _ => "Обновить" };
        public string WeatherWidgetHumidity => CurrentLanguage switch { "EN" => "Humidity", "ZH" => "湿度", _ => "Влажность" };
        public string WeatherWindSpeed => CurrentLanguage switch { "EN" => "Wind speed", "ZH" => "风速", _ => "Скорость ветра" };

        // Tweaks Page
        public string TweakSubheader => CurrentLanguage switch { "EN" => "Configure interface and Windows parameters in one click", "ZH" => "一键配置界面和 Windows 参数", _ => "Настройка интерфейса и параметров Windows в один клик" };
        public string TweakAdminDesc => CurrentLanguage switch { "EN" => "Some settings require administrator privileges. Run as administrator?", "ZH" => "某些设置需要管理员权限。以管理员身份运行吗？", _ => "Некоторые настройки требуют прав администратора. Запустить от имени администратора?" };
        public string TweakRestartBtn => CurrentLanguage switch { "EN" => "Restart App", "ZH" => "重新启动", _ => "Перезапустить" };
        public string TweakDynamicIslandTitle => CurrentLanguage switch { "EN" => "Dynamic Island Settings", "ZH" => "灵动岛设置", _ => "Настройки Dynamic Island" };
        public string TweakDynamicIslandDesc => CurrentLanguage switch { "EN" => "Show the smart Dynamic Island panel at the top of the screen and configure active modules.", "ZH" => "在屏幕顶部显示智能灵动岛面板并配置活动模块。", _ => "Показать умную панель Dynamic Island вверху экрана и настроить активные модули." };
        public string TweakDiWidth => CurrentLanguage switch { "EN" => "Width when collapsed:", "ZH" => "折叠状态下的宽度:", _ => "Ширина в сложенном состоянии:" };
        public string TweakDiTopMargin => CurrentLanguage switch { "EN" => "Top margin:", "ZH" => "顶部间距:", _ => "Отступ сверху:" };
        public string TweakDiScreen => CurrentLanguage switch { "EN" => "Select screen:", "ZH" => "选择屏幕:", _ => "Выбор экрана:" };
        public string TweakDiModules => CurrentLanguage switch { "EN" => "Enabled modules:", "ZH" => "启用的模块:", _ => "Включенные модули:" };
        public string TweakModuleMusic => CurrentLanguage switch { "EN" => "Music player", "ZH" => "音乐播放器", _ => "Музыкальный плеер" };
        public string TweakModuleOverheat => CurrentLanguage switch { "EN" => "Overheat warning", "ZH" => "过热警告", _ => "Предупреждение о перегреве" };
        public string TweakModuleFocus => CurrentLanguage switch { "EN" => "Focus timer (Pomodoro)", "ZH" => "专注番茄钟", _ => "Таймер фокуса (Pomodoro)" };
        public string TweakModuleScreenshot => CurrentLanguage switch { "EN" => "Screenshot preview", "ZH" => "屏幕截图预览", _ => "Превью снимков экрана" };
        public string TweakModuleVpn => CurrentLanguage switch { "EN" => "VPN indicator", "ZH" => "VPN 指示器", _ => "Индикатор VPN" };
        public string TweakModuleCamMic => CurrentLanguage switch { "EN" => "Camera/Microphone indicators", "ZH" => "摄像头/麦克风指示器", _ => "Индикаторы Камеры/Микрофона" };
        public string TweakCurrentFont => CurrentLanguage switch { "EN" => "Current font: ", "ZH" => "当前字体: ", _ => "Текущий шрифт: " };
        public string TweakFontSelectApply => CurrentLanguage switch { "EN" => "Select and apply (.ttf/.otf)", "ZH" => "选择并应用 (.ttf/.otf)", _ => "Выбрать и применить (.ttf/.otf)" };
        public string TweakFontRestoreDefault => CurrentLanguage switch { "EN" => "Restore default", "ZH" => "恢复默认", _ => "Вернуть стандартный" };
        public string TweakFontRestartRequired => CurrentLanguage switch { "EN" => "Restart is required", "ZH" => "需要重启电脑", _ => "Требуется перезагрузка компьютера" };
        public string TweakFontRestartDesc => CurrentLanguage switch { "EN" => "To fully apply system font changes, you need to restart your computer.", "ZH" => "为了完全应用系统字体更改，您需要重启电脑。", _ => "Для полного применения изменений шрифта необходимо перезапустить компьютер." };
        public string TweakFontRestartNow => CurrentLanguage switch { "EN" => "Restart now", "ZH" => "立即重启", _ => "Перезагрузить сейчас" };
        public string TweakFontDisclaimer => CurrentLanguage switch { "EN" => "* Computer restart is required to fully apply the system font.", "ZH" => "* 需要重启电脑以完全应用系统字体。", _ => "* Для полного применения системного шрифта требуется перезагрузка ПК." };
        public string TweakStatusAdminRequired => CurrentLanguage switch { "EN" => "Administrator privileges are required!", "ZH" => "需要管理员权限！", _ => "Требуются права Администратора!" };
        public string TweakStatusFontApplied => CurrentLanguage switch { "EN" => "Font applied successfully! Please restart the computer.", "ZH" => "字体应用成功！请重启电脑。", _ => "Шрифт успешно применен! Перезапустите ПК." };
        public string TweakStatusFontError => CurrentLanguage switch { "EN" => "Error selecting/applying font", "ZH" => "选择/应用字体时出错", _ => "Ошибка при выборе/применении шрифта" };
        public string TweakStatusFontRestored => CurrentLanguage switch { "EN" => "Default font restored! Please restart the computer.", "ZH" => "默认字体已恢复！请重启电脑。", _ => "Стандартный шрифт восстановлен! Перезапустите ПК." };
        public string TweakStatusFontRestoreError => CurrentLanguage switch { "EN" => "Error restoring font", "ZH" => "恢复字体时出错", _ => "Ошибка при восстановлении шрифта" };
        public string TweakStatusSoundsEnabled => CurrentLanguage switch { "EN" => "Windows system sounds enabled!", "ZH" => "Windows 系统声音已启用！", _ => "Системные звуки Windows включены!" };
        public string TweakStatusSoundsDisabled => CurrentLanguage switch { "EN" => "Windows system sounds disabled!", "ZH" => "Windows 系统声音已禁用！", _ => "Системные звуки Windows отключены!" };
        public string TweakStatusPasswordChanged => CurrentLanguage switch { "EN" => "App lock password changed!", "ZH" => "应用锁密码已更改！", _ => "Пароль приложения изменен!" };
        public string TweakStatusAppAdded => CurrentLanguage switch { "EN" => "App added to block list!", "ZH" => "应用已添加到锁定列表！", _ => "Приложение добавлено в список блокировки!" };
        public string TweakStatusAppRemoved => CurrentLanguage switch { "EN" => "App removed from block list!", "ZH" => "应用已从锁定列表移除！", _ => "Приложение удалено из списка блокировки!" };
        public string TweakScreenLabel => CurrentLanguage switch { "EN" => "Screen", "ZH" => "屏幕", _ => "Экран" };
        public string TweakPrimaryLabel => CurrentLanguage switch { "EN" => " (Primary)", "ZH" => " (主屏幕)", _ => " (Основной)" };

        // Tools Page
        public string ToolsSubheader => CurrentLanguage switch { "EN" => "A collection of helpful micro-utilities for work and focus", "ZH" => "一系列有助于工作和专注的实用微工具", _ => "Набор полезных микро-утилит для работы и фокуса" };
        public string ToolsTabFocus => CurrentLanguage switch { "EN" => "Focus", "ZH" => "专注", _ => "Фокус" };
        public string ToolsTabUtils => CurrentLanguage switch { "EN" => "Utilities", "ZH" => "工具", _ => "Утилиты" };
        public string ToolsFocusWork => CurrentLanguage switch { "EN" => "Work (minutes):", "ZH" => "工作（分钟）:", _ => "Работа (минут):" };
        public string ToolsFocusBreak => CurrentLanguage switch { "EN" => "Break (minutes):", "ZH" => "休息（分钟）:", _ => "Отдых (минут):" };
        public string ToolsMailPlaceholder => CurrentLanguage switch { "EN" => "Click 'Generate Inbox'", "ZH" => "点击 '生成邮箱'", _ => "Нажмите 'Создать ящик'" };
        public string ToolsMailCopy => CurrentLanguage switch { "EN" => "Copy Address", "ZH" => "复制地址", _ => "Копировать адрес" };
        public string ToolsMailIncoming => CurrentLanguage switch { "EN" => "Incoming Messages:", "ZH" => "收件箱邮件:", _ => "Входящие сообщения:" };
        public string ToolsCalcTitle => CurrentLanguage switch { "EN" => "Calculator", "ZH" => "计算器", _ => "Калькулятор" };
        public string ToolsClocksTitle => CurrentLanguage switch { "EN" => "Time & Timers", "ZH" => "时间与定时器", _ => "Время и Таймеры" };
        public string ToolsClockTabClocks => CurrentLanguage switch { "EN" => "Clock", "ZH" => "时钟", _ => "Часы" };
        public string ToolsClockTabStopwatch => CurrentLanguage switch { "EN" => "Stopwatch", "ZH" => "秒表", _ => "Секундомер" };
        public string ToolsClockTabTimer => CurrentLanguage switch { "EN" => "Timer", "ZH" => "定时器", _ => "Таймер" };
        public string ToolsClockLocalTime => CurrentLanguage switch { "EN" => "Local Time", "ZH" => "本地时间", _ => "Местное время" };
        public string ToolsClockCurrentLocation => CurrentLanguage switch { "EN" => "Current Location", "ZH" => "当前位置", _ => "Текущее местоположение" };
        public string ToolsClockNewYork => CurrentLanguage switch { "EN" => "New York", "ZH" => "纽约", _ => "Нью-Йорк" };
        public string ToolsClockLondon => CurrentLanguage switch { "EN" => "London", "ZH" => "伦敦", _ => "Лондон" };
        public string ToolsClockTokyo => CurrentLanguage switch { "EN" => "Tokyo", "ZH" => "东京", _ => "Токио" };
        public string ToolsClockStart => CurrentLanguage switch { "EN" => "Start", "ZH" => "开始", _ => "Старт" };
        public string ToolsClockStop => CurrentLanguage switch { "EN" => "Stop", "ZH" => "停止", _ => "Стоп" };
        public string ToolsClockReset => CurrentLanguage switch { "EN" => "Reset", "ZH" => "复位", _ => "Сбросить" };
        public string ToolsTimerTimeRemaining => CurrentLanguage switch { "EN" => "Time Remaining", "ZH" => "剩余时间", _ => "Оставшееся время" };
        public string ToolsTimerMinutesInput => CurrentLanguage switch { "EN" => "Minutes:", "ZH" => "分钟:", _ => "Минут:" };
        public string ToolsTimerPause => CurrentLanguage switch { "EN" => "Pause", "ZH" => "暂停", _ => "Пауза" };
        public string ToolsTimerReset => CurrentLanguage switch { "EN" => "Reset", "ZH" => "重置", _ => "Сброс" };
        public string ToolsConverterTitle => CurrentLanguage switch { "EN" => "Image Converter", "ZH" => "图片转换器", _ => "Конвертер изображений" };
        public string ToolsConverterDragPrompt => CurrentLanguage switch { "EN" => "Drag and drop image here", "ZH" => "将图片拖放到这里", _ => "Перетащите изображение сюда" };
        public string ToolsConverterDragReplace => CurrentLanguage switch { "EN" => "Drag another one to replace", "ZH" => "拖放另一张进行替换", _ => "Перетащите другое для замены" };
        public string ToolsConverterSelectFile => CurrentLanguage switch { "EN" => "Select File...", "ZH" => "选择文件...", _ => "Выбрать файл..." };
        public string ToolsConverterClear => CurrentLanguage switch { "EN" => "Clear", "ZH" => "清除", _ => "Очистить" };
        public string ToolsConverterConvertFormat => CurrentLanguage switch { "EN" => "Quick convert to format:", "ZH" => "快速转换为格式:", _ => "Быстрое конвертирование в формат:" };
        public string ToolsMailCreatingInbox => CurrentLanguage switch { "EN" => "Creating inbox...", "ZH" => "正在生成邮箱...", _ => "Создание ящика..." };
        public string ToolsMailAuthError => CurrentLanguage switch { "EN" => "Error: Failed to authenticate.", "ZH" => "错误: 身份验证失败。", _ => "Ошибка: не удалось авторизоваться." };
        public string ToolsMailDomainError => CurrentLanguage switch { "EN" => "Error: Domains unavailable.", "ZH" => "错误: 域名不可用。", _ => "Ошибка: домены недоступны." };
        public string ToolsMailNetworkError => CurrentLanguage switch { "EN" => "Network error: ", "ZH" => "网络错误: ", _ => "Ошибка сети: " };
        public string ToolsShredderErasing => CurrentLanguage switch { "EN" => "Shredding files...", "ZH" => "正在粉碎文件...", _ => "Затирание файлов..." };
        public string ToolsShredderError => CurrentLanguage switch { "EN" => "Error: ", "ZH" => "错误: ", _ => "Ошибка: " };
        public string ToolsDaysPluralZeroOne => CurrentLanguage switch { "EN" => "days", "ZH" => "天", _ => "дней" };
        public string ToolsDaysPluralTwoFour => CurrentLanguage switch { "EN" => "days", "ZH" => "天", _ => "дня" };
        public string ToolsDaysPluralMany => CurrentLanguage switch { "EN" => "days", "ZH" => "天", _ => "дней" };
        public string ToolsDaysPluralOne => CurrentLanguage switch { "EN" => "day", "ZH" => "天", _ => "день" };
        public string ToolsClocksOffsetMatchesLocal => CurrentLanguage switch { "EN" => "same as local", "ZH" => "与本地一致", _ => "совпадает с местным" };
        public string ToolsClocksOffsetHourFromLocal => CurrentLanguage switch { "EN" => "h from local", "ZH" => "小时（相比本地）", _ => "ч от местного" };
        public string ToolsTimerCountdownDone => CurrentLanguage switch { "EN" => "Countdown timer finished!", "ZH" => "倒计时已结束！", _ => "Таймер обратного отсчета завершен!" };
        public string ToolsOcrHighlightPrompt => CurrentLanguage switch { "EN" => "Select an area on the screen...", "ZH" => "在屏幕上选择一个区域...", _ => "Выделите область на экране..." };
        public string ToolsOcrRecognizing => CurrentLanguage switch { "EN" => "Recognizing text...", "ZH" => "正在识别文本...", _ => "Распознавание текста..." };
        public string ToolsOcrNotFound => CurrentLanguage switch { "EN" => "Text not found in the selected area.", "ZH" => "选定区域内未检测到文本。", _ => "Текст в выделенной области не обнаружен." };
        public string ToolsOcrCopiedSuccess => CurrentLanguage switch { "EN" => "Text copied to clipboard successfully!", "ZH" => "文本已成功复制到剪贴板！", _ => "Текст успешно скопирован в буфер обмена!" };
        public string ToolsOcrPackNotInstalled => CurrentLanguage switch { "EN" => "OCR language pack is not installed in Windows.", "ZH" => "Windows 中未安装 OCR 语言包。", _ => "Языковой пакет OCR не установлен в Windows." };
        public string ToolsOcrProcessingError => CurrentLanguage switch { "EN" => "Failed to process selected area.", "ZH" => "无法处理选定区域。", _ => "Не удалось обработать выделенную область." };
        public string ToolsOcrGenericError => CurrentLanguage switch { "EN" => "OCR Error: ", "ZH" => "OCR 错误: ", _ => "Ошибка OCR: " };
        public string ToolsOcrSelectionCanceled => CurrentLanguage switch { "EN" => "Selection canceled.", "ZH" => "选择已取消。", _ => "Выделение отменено." };
        public string ToolsCableInstalling => CurrentLanguage switch { "EN" => "Installing virtual cable...", "ZH" => "正在安装虚拟电缆...", _ => "Установка виртуального кабеля..." };
        public string ToolsCableDownloading => CurrentLanguage switch { "EN" => "Downloading driver (vb-audio.com)...", "ZH" => "正在下载驱动程序 (vb-audio.com)...", _ => "Скачивание драйвера (vb-audio.com)..." };
        public string ToolsCableExtracting => CurrentLanguage switch { "EN" => "Unpacking installer...", "ZH" => "正在解压安装程序...", _ => "Распаковка установщика..." };
        public string ToolsCableLaunchingUac => CurrentLanguage switch { "EN" => "Running installer (allow in UAC)...", "ZH" => "正在运行安装程序（请在 UAC 中允许）...", _ => "Запуск установки (разрешите в окне UAC)..." };
        public string ToolsCableInstallSuccess => CurrentLanguage switch { "EN" => "Installation completed successfully!", "ZH" => "安装成功完成！", _ => "Установка успешно завершена!" };
        public string ToolsCableInstallError => CurrentLanguage switch { "EN" => "Installation error: ", "ZH" => "安装出错: ", _ => "Ошибка установки: " };
        public string ToolsCableNotInstalled => CurrentLanguage switch { "EN" => "Virtual cable is not installed", "ZH" => "未安装虚拟电缆", _ => "Виртуальный кабель не установлен" };
        public string ToolsPlayerNoFile => CurrentLanguage switch { "EN" => "No file selected", "ZH" => "未选择文件", _ => "Не выбран" };
        public string ToolsPlayerError => CurrentLanguage switch { "EN" => "Playback error: ", "ZH" => "播放错误: ", _ => "Ошибка воспроизведения: " };
        public string ToolsPlayerFileNotSelected => CurrentLanguage switch { "EN" => "File not selected", "ZH" => "文件未选择", _ => "Файл не выбран" };
        public string ToolsConverterImageSuccess => CurrentLanguage switch { "EN" => "Image converted successfully!", "ZH" => "图片转换成功！", _ => "Изображение успешно сконвертировано!" };
        public string ToolsConverterError => CurrentLanguage switch { "EN" => "Conversion error: ", "ZH" => "转换错误: ", _ => "Ошибка конвертирования: " };
        public string ToolsActionCanceled => CurrentLanguage switch { "EN" => "Action canceled.", "ZH" => "操作已取消。", _ => "Действие отменено." };
        public string ToolsFileNotSelected => CurrentLanguage switch { "EN" => "File not selected", "ZH" => "文件未选择", _ => "Файл не выбран" };
        public string ToolsOcrFileSelectorTitle => CurrentLanguage switch { "EN" => "Select executable file (.exe)", "ZH" => "选择可执行文件 (.exe)", _ => "Выберите исполняемый файл (.exe)" };
        public string ToolsOcrFileSelectorApps => CurrentLanguage switch { "EN" => "Applications", "ZH" => "程序", _ => "Программы" };
        public string ToolsFileFontPickerTitle => CurrentLanguage switch { "EN" => "Select font file", "ZH" => "选择字体文件", _ => "Выберите файл шрифта" };
        public string ToolsFileFontPickerFilter => CurrentLanguage switch { "EN" => "Font Files (*.ttf, *.otf)", "ZH" => "字体文件 (*.ttf, *.otf)", _ => "Файлы шрифтов (*.ttf, *.otf)" };
        public string ToolsWallpapersPickerTitle => CurrentLanguage switch { "EN" => "Select wallpaper file (.mp4, .html)", "ZH" => "选择壁纸文件 (.mp4, .html)", _ => "Выберите файл обоев (.mp4, .html)" };
        public string ToolsWallpapersPickerFilter => CurrentLanguage switch { "EN" => "Wallpaper Files", "ZH" => "壁纸文件", _ => "Файлы обоев" };
        public string ToolsError => CurrentLanguage switch { "EN" => "Error", "ZH" => "错误", _ => "Ошибка" };

        // App Lock (PasswordPromptWindow & LockScreenView)
        public string AppLockTitle => CurrentLanguage switch { "EN" => "SystemHub - App Protection", "ZH" => "SystemHub - 应用保护", _ => "SystemHub - Защита приложений" };
        public string AppLockAccessDenied => CurrentLanguage switch { "EN" => "Access Blocked", "ZH" => "Доступ заблокирован", _ => "Доступ заблокирован" };
        public string AppLockEnterPassword => CurrentLanguage switch { "EN" => "Enter password", "ZH" => "输入密码", _ => "Введите пароль" };
        public string AppLockInvalidPassword => CurrentLanguage switch { "EN" => "Invalid password!", "ZH" => "密码错误！", _ => "Неверный пароль!" };
        public string AppLockClose => CurrentLanguage switch { "EN" => "Close", "ZH" => "关闭", _ => "Закрыть" };
        public string AppLockUnlock => CurrentLanguage switch { "EN" => "Unlock", "ZH" => "解锁", _ => "Разблокировать" };
        public string AppLockPromptFormat => CurrentLanguage switch { "EN" => "Launch of application \"{0}\" is temporarily suspended. Enter password to continue.", "ZH" => "应用程序 \"{0}\" 的启动已被临时挂起。请输入密码以继续。", _ => "Запуск приложения \"{0}\" временно приостановлен. Введите пароль для продолжения." };
        public string AppLockScreenTitle => CurrentLanguage switch { "EN" => "Application Blocked", "ZH" => "应用已锁定", _ => "Приложение заблокировано" };
        public string AppLockInputPlaceholder => CurrentLanguage switch { "EN" => "Enter password", "ZH" => "输入密码", _ => "Введите пароль" };

        // Profile (ProfileView & ProfileViewModel)
        public string ProfileTitle => CurrentLanguage switch { "EN" => "User Profile", "ZH" => "个人中心", _ => "Личный кабинет" };
        public string ProfileRegDateLabel => CurrentLanguage switch { "EN" => "Registration Date:", "ZH" => "注册日期:", _ => "Дата регистрации:" };
        public string ProfileEditHeader => CurrentLanguage switch { "EN" => "Edit Profile", "ZH" => "编辑个人资料", _ => "Редактировать профиль" };
        public string ProfileSelectPhoto => CurrentLanguage switch { "EN" => "Select Photo", "ZH" => "选择照片", _ => "Выбрать фото" };
        public string ProfileDeletePhoto => CurrentLanguage switch { "EN" => "Delete Photo", "ZH" => "删除照片", _ => "Удалить фото" };
        public string ProfileUsernameLabel => CurrentLanguage switch { "EN" => "Username:", "ZH" => "用户名:", _ => "Имя пользователя:" };
        public string ProfileUsernamePlaceholder => CurrentLanguage switch { "EN" => "Your nickname...", "ZH" => "您的昵称...", _ => "Ваш ник..." };
        public string ProfileSaveBtn => CurrentLanguage switch { "EN" => "Save", "ZH" => "保存", _ => "Сохранить" };
        public string ProfileSecurityHeader => CurrentLanguage switch { "EN" => "Security", "ZH" => "安全设置", _ => "Безопасность" };
        public string ProfileOldPasswordLabel => CurrentLanguage switch { "EN" => "Current Password:", "ZH" => "当前密码:", _ => "Текущий пароль:" };
        public string ProfileOldPasswordPlaceholder => CurrentLanguage switch { "EN" => "Enter current password...", "ZH" => "输入当前密码...", _ => "Введите текущий пароль..." };
        public string ProfileNewPasswordLabel => CurrentLanguage switch { "EN" => "New Password:", "ZH" => "新密码:", _ => "Новый пароль:" };
        public string ProfileNewPasswordPlaceholder => CurrentLanguage switch { "EN" => "New password...", "ZH" => "新密码...", _ => "Новый пароль..." };
        public string ProfileConfirmPasswordLabel => CurrentLanguage switch { "EN" => "Confirm Password:", "ZH" => "确认密码:", _ => "Подтверждение:" };
        public string ProfileConfirmPasswordPlaceholder => CurrentLanguage switch { "EN" => "Repeat new password...", "ZH" => "重复新密码...", _ => "Повторите новый пароль..." };
        public string ProfileChangePasswordBtn => CurrentLanguage switch { "EN" => "Change Password", "ZH" => "修改密码", _ => "Изменить пароль" };
        public string ProfileAuthSettingsHeader => CurrentLanguage switch { "EN" => "Authentication & Interface Settings", "ZH" => "认证与界面设置", _ => "Настройки авторизации и интерфейса" };
        public string ProfileAutoLoginTitle => CurrentLanguage switch { "EN" => "Automatic Login", "ZH" => "自动登录", _ => "Автоматический вход" };
        public string ProfileAutoLoginDesc => CurrentLanguage switch { "EN" => "Remember me on this device for automatic login on startup", "ZH" => "在此设备上记住我，以便在启动时自动登录", _ => "Запомнить меня на этом устройстве для автоматического входа при запуске" };
        public string ProfileShowNicknameTitle => CurrentLanguage switch { "EN" => "Show Nickname in Greetings", "ZH" => "在问候中显示昵称", _ => "Показывать никнейм в приветствиях" };
        public string ProfileShowNicknameDesc => CurrentLanguage switch { "EN" => "Display your name in the welcome screen when the application launches", "ZH" => "应用程序启动时在欢迎屏幕中显示您的名字", _ => "Отображать ваше имя в приветственном окне при запуске приложения" };
        public string ProfileLogoutHeader => CurrentLanguage switch { "EN" => "End Session", "ZH" => "结束会话", _ => "Завершение сеанса" };
        public string ProfileLogoutDesc => CurrentLanguage switch { "EN" => "Log out of the current account and return to the login screen", "ZH" => "退出当前账户并返回登录页面", _ => "Выйти из текущего аккаунта и вернуться к экрану авторизации" };
        public string ProfileLogoutBtn => CurrentLanguage switch { "EN" => "Log Out", "ZH" => "退出", _ => "Выйти" };
        public string ProfileSelectAvatarTitle => CurrentLanguage switch { "EN" => "Select Avatar", "ZH" => "选择头像", _ => "Выберите аватар" };
        public string ProfileImagesFilter => CurrentLanguage switch { "EN" => "Images", "ZH" => "图片", _ => "Изображения" };
        public string ProfileSavedSuccess => CurrentLanguage switch { "EN" => "Profile saved successfully", "ZH" => "个人资料保存成功", _ => "Профиль сохранен" };
        public string ProfileFillAllFields => CurrentLanguage switch { "EN" => "Please fill in all password fields", "ZH" => "请填写所有密码字段", _ => "Заполните все поля паролей" };
        public string ProfilePasswordsMismatch => CurrentLanguage switch { "EN" => "New passwords do not match", "ZH" => "新密码不匹配", _ => "Новые пароли не совпадают" };
        public string ProfilePasswordChangedSuccess => CurrentLanguage switch { "EN" => "Password changed successfully", "ZH" => "密码修改成功", _ => "Пароль успешно изменен" };

        // System Info (SystemInfoView & SystemInfoViewModel)
        public string SysInfoTempHistory => CurrentLanguage switch { "EN" => "Temperature History:", "ZH" => "温度历史:", _ => "История температуры:" };
        public string SysInfoTotal => CurrentLanguage switch { "EN" => "Total:", "ZH" => "总量:", _ => "Всего:" };
        public string SysInfoNetworkSpeed => CurrentLanguage switch { "EN" => "Network Speed", "ZH" => "网络速度", _ => "Скорость сети" };
        public string SysInfoDownload => CurrentLanguage switch { "EN" => "Download", "ZH" => "загрузка", _ => "Загрузка" };
        public string SysInfoUpload => CurrentLanguage switch { "EN" => "Upload", "ZH" => "отдача", _ => "Отдача" };
        public string SysInfoMotherboardShort => CurrentLanguage switch { "EN" => "Board:", "ZH" => "主板:", _ => "Плата:" };
        public string SysInfoFan => CurrentLanguage switch { "EN" => "Fan:", "ZH" => "风扇:", _ => "Вентилятор:" };
        public string SysInfoTopProcesses => CurrentLanguage switch { "EN" => "Heavyweights (Top 5 Processes)", "ZH" => "高负载进程 (前5个)", _ => "Тяжеловесы (Топ-5 процессов)" };
        public string SysInfoProcessCol => CurrentLanguage switch { "EN" => "Process", "ZH" => "进程", _ => "Процесс" };
        public string SysInfoCpuCol => CurrentLanguage switch { "EN" => "CPU", "ZH" => "ЦПУ", _ => "ЦПУ" };
        public string SysInfoRamCol => CurrentLanguage switch { "EN" => "Memory", "ZH" => "内存", _ => "Память" };
        public string SysInfoDiskBenchmark => CurrentLanguage switch { "EN" => "Disk Speed Test (Benchmark)", "ZH" => "磁盘速度测试", _ => "Тест скорости дисков (Benchmark)" };
        public string SysInfoBenchmarkDesc => CurrentLanguage switch { "EN" => "Run disk performance benchmark using the button in the drives list below. Test measures read and write speeds of a 100 MB file.", "ZH" => "点击下方磁盘列表中的按钮运行磁盘性能基准测试。测试将测量 100 MB 文件的读写速度。", _ => "Запустите тест производительности диска с помощью кнопки в списке дисков ниже. Тест замерит скорость записи и чтения файла 100 МБ." };
        public string SysInfoSeqWrite => CurrentLanguage switch { "EN" => "Write (Sequential)", "ZH" => "写入 (顺序)", _ => "Запись (Sequential Write)" };
        public string SysInfoSeqRead => CurrentLanguage switch { "EN" => "Read (Sequential)", "ZH" => "读取 (顺序)", _ => "Чтение (Sequential Read)" };
        public string SysInfoBenchmarkBtn => CurrentLanguage switch { "EN" => "Test", "ZH" => "测试", _ => "Тест" };
        public string SysInfoMissing => CurrentLanguage switch { "EN" => "Missing", "ZH" => "不存在", _ => "Отсутствует" };
        public string SysInfoUndefined => CurrentLanguage switch { "EN" => "Undefined", "ZH" => "未定义", _ => "Не определена" };
        public string SysInfoBenchmarkPreparing => CurrentLanguage switch { "EN" => "Preparing...", "ZH" => "准备中...", _ => "Подготовка..." };
        public string SysInfoBenchmarkWriting => CurrentLanguage switch { "EN" => "Writing test...", "ZH" => "写入测试中...", _ => "Тест записи..." };
        public string SysInfoBenchmarkReading => CurrentLanguage switch { "EN" => "Reading test...", "ZH" => "读取测试中...", _ => "Тест чтения..." };
        public string SysInfoBenchmarkCompleted => CurrentLanguage switch { "EN" => "Completed", "ZH" => "已完成", _ => "Завершено" };
        public string SysInfoWriteError => CurrentLanguage switch { "EN" => "Failed to write test file: ", "ZH" => "无法写入测试文件: ", _ => "Не удалось записать тестовый файл: " };
        public string SysInfoReadError => CurrentLanguage switch { "EN" => "Failed to read test file: ", "ZH" => "无法读取测试文件: ", _ => "Не удалось прочитать тестовый файл: " };

        // Media Playback (MediaPlaybackView & MediaPlaybackViewModel)
        public string MediaEqTooltip => CurrentLanguage switch { "EN" => "Open/close equalizer", "ZH" => "打开/关闭均衡器", _ => "Открыть/закрыть эквалайзер" };
        public string MediaEqHeader => CurrentLanguage switch { "EN" => "Equalizer", "ZH" => "均衡器", _ => "Эквалайзер" };
        public string MediaVisualizerHeader => CurrentLanguage switch { "EN" => "Spectrum Visualizer", "ZH" => "频谱可视化", _ => "Визуализатор спектра" };
        public string MediaVisualizerDesc => CurrentLanguage switch { "EN" => "Analysis and display of system sound playback frequencies", "ZH" => "系统音频播放频率的分析与显示", _ => "Анализ и отображение частот воспроизведения системного звука" };
        public string MediaEqPresetsHeader => CurrentLanguage switch { "EN" => "Equalizer Presets:", "ZH" => "均衡器预设:", _ => "Пресеты эквалайзера:" };
        public string MediaSavePresetTooltip => CurrentLanguage switch { "EN" => "Save changes to preset", "ZH" => "保存更改到预设", _ => "Сохранить изменения в пресет" };
        public string MediaDeletePresetTooltip => CurrentLanguage switch { "EN" => "Delete preset", "ZH" => "删除预设", _ => "Удалить пресет" };
        public string MediaImportPresetTooltip => CurrentLanguage switch { "EN" => "Import preset (.json)", "ZH" => "导入预设 (.json)", _ => "Импортировать пресет (.json)" };
        public string MediaExportPresetTooltip => CurrentLanguage switch { "EN" => "Export selected preset (.json)", "ZH" => "导出所选预设 (.json)", _ => "Экспортировать выбранный пресет (.json)" };
        public string MediaNewPresetPlaceholder => CurrentLanguage switch { "EN" => "New preset name...", "ZH" => "新预设名称...", _ => "Имя нового пресета..." };
        public string MediaSaveBtn => CurrentLanguage switch { "EN" => "Save", "ZH" => "保存", _ => "Сохранить" };
        public string MediaOutputDeviceLabel => CurrentLanguage switch { "EN" => "Output Device:", "ZH" => "输出设备:", _ => "Устройство вывода:" };
        public string MediaEchoNotice => CurrentLanguage switch { "EN" => "If you hear echo/duplication: in Windows, open recording properties of 'CABLE Output' -> 'Listen' tab -> uncheck 'Listen to this device'.", "ZH" => "如果您听到回声/重复：请在 Windows 中打开“CABLE Output”的录制属性 -> “监听”选项卡 -> 取消勾选“监听此设备”。", _ => "Если слышно эхо/дублирование: в Windows откройте свойства записи 'CABLE Output' -> вкладка 'Прослушивать' -> снимите флажок 'Прослушивать с данного устройства'." };
        public string MediaMasterLevelLabel => CurrentLanguage switch { "EN" => "level", "ZH" => "电平", _ => "уровень" };
        public string MediaSystemSounds => CurrentLanguage switch { "EN" => "System Sounds", "ZH" => "系统声音", _ => "Системные звуки" };
        public string MediaNothingPlaying => CurrentLanguage switch { "EN" => "Nothing Playing", "ZH" => "未播放任何内容", _ => "Ничего не играет" };
        public string MediaYandexMusic => CurrentLanguage switch { "EN" => "Yandex.Music", "ZH" => "Yandex 音乐", _ => "Яндекс.Музыка" };

        // Main Window (MainWindow & MainWindowViewModel)
        public string MainSidebarToggleTooltip => CurrentLanguage switch { "EN" => "Collapse/Expand sidebar", "ZH" => "折叠/展开侧边栏", _ => "Свернуть/Развернуть боковую панель" };
        public string MainGreetingMorning => CurrentLanguage switch { "EN" => "Good morning", "ZH" => "早上好", _ => "Доброе утро" };
        public string MainGreetingAfternoon => CurrentLanguage switch { "EN" => "Good afternoon", "ZH" => "下午好", _ => "Добрый день" };
        public string MainGreetingEvening => CurrentLanguage switch { "EN" => "Good evening", "ZH" => "晚上好", _ => "Добрый вечер" };
        public string MainGreetingNight => CurrentLanguage switch { "EN" => "Good night", "ZH" => "晚安", _ => "Доброй ночи" };
        public string MainWelcomeSubtext => CurrentLanguage switch { "EN" => "Welcome to SystemHub", "ZH" => "欢迎使用 SystemHub", _ => "Добро пожаловать в SystemHub" };

        // Auth (AuthView & AuthViewModel)
        public string AuthLoginTitle => CurrentLanguage switch { "EN" => "Sign In", "ZH" => "登录账号", _ => "Вход в аккаунт" };
        public string AuthRegisterTitle => CurrentLanguage switch { "EN" => "Register", "ZH" => "用户注册", _ => "Регистрация" };
        public string AuthVerifyEmailTitle => CurrentLanguage switch { "EN" => "Confirm Email", "ZH" => "验证邮箱", _ => "Подтверждение почты" };
        public string AuthForgotPasswordTitle => CurrentLanguage switch { "EN" => "Reset Password", "ZH" => "重置密码", _ => "Восстановление доступа" };
        public string AuthResetPasswordTitle => CurrentLanguage switch { "EN" => "New Password", "ZH" => "新密码", _ => "Новый пароль" };
        public string AuthTabLogin => CurrentLanguage switch { "EN" => "Login", "ZH" => "登录", _ => "Вход" };
        public string AuthTabRegister => CurrentLanguage switch { "EN" => "Register", "ZH" => "注册", _ => "Регистрация" };
        public string AuthEmailUserLabel => CurrentLanguage switch { "EN" => "Email or Username:", "ZH" => "电子邮箱或用户名:", _ => "Почта или имя пользователя:" };
        public string AuthEmailUserPlaceholder => CurrentLanguage switch { "EN" => "Enter email or username...", "ZH" => "请输入邮箱或用户名...", _ => "Введите почту или ник..." };
        public string AuthPasswordLabel => CurrentLanguage switch { "EN" => "Password:", "ZH" => "密码:", _ => "Пароль:" };
        public string AuthPasswordPlaceholder => CurrentLanguage switch { "EN" => "Enter password...", "ZH" => "请输入密码...", _ => "Введите пароль..." };
        public string AuthRememberMe => CurrentLanguage switch { "EN" => "Remember Me", "ZH" => "记住我", _ => "Запомнить меня" };
        public string AuthForgotPasswordLink => CurrentLanguage switch { "EN" => "Forgot Password?", "ZH" => "忘记密码？", _ => "Забыли пароль?" };
        public string AuthUsernameLabel => CurrentLanguage switch { "EN" => "Username:", "ZH" => "用户名:", _ => "Имя пользователя:" };
        public string AuthUsernamePlaceholder => CurrentLanguage switch { "EN" => "Username...", "ZH" => "用户名...", _ => "Имя пользователя..." };
        public string AuthEmailLabel => CurrentLanguage switch { "EN" => "Email address:", "ZH" => "电子邮箱:", _ => "Адрес электронной почты (Email):" };
        public string AuthPasswordNewPlaceholder => CurrentLanguage switch { "EN" => "Enter new password...", "ZH" => "请输入新密码...", _ => "Введите новый пароль..." };
        public string AuthVerifyEmailDesc => CurrentLanguage switch { "EN" => "We sent a 6-digit confirmation code to your email. Please enter it below to complete registration.", "ZH" => "我们已向您的邮箱发送了 6 位验证码。请在下方输入以完成注册。", _ => "Мы отправили 6-значный код подтверждения на вашу почту. Пожалуйста, введите его ниже для завершения регистрации." };
        public string AuthVerifyCodeLabel => CurrentLanguage switch { "EN" => "Verification Code:", "ZH" => "验证码:", _ => "Код подтверждения:" };
        public string AuthVerifyCodePlaceholder => CurrentLanguage switch { "EN" => "Enter 6 digits...", "ZH" => "请输入 6 位数字...", _ => "Введите 6 цифр..." };
        public string AuthForgotPasswordDesc => CurrentLanguage switch { "EN" => "Enter your registered email address. We will send you a reset code.", "ZH" => "输入您注册的电子邮箱地址。我们将向您发送重置密码的验证码。", _ => "Введите зарегистрированный адрес электронной почты. Мы вышлем вам код сброса пароля." };
        public string AuthEmailResetLabel => CurrentLanguage switch { "EN" => "Email address:", "ZH" => "Email 地址:", _ => "Email адрес:" };
        public string AuthResetPasswordDesc => CurrentLanguage switch { "EN" => "Reset code sent. Please enter it below along with a new password.", "ZH" => "密码重置验证码已发送。请在下方输入验证码及新密码。", _ => "Код восстановления отправлен. Пожалуйста, введите его ниже вместе с новым паролем." };
        public string AuthResetCodeLabel => CurrentLanguage switch { "EN" => "Reset Code:", "ZH" => "重置验证码:", _ => "Код сброса пароля:" };
        public string AuthResetCodePlaceholder => CurrentLanguage switch { "EN" => "Enter 6 digits...", "ZH" => "请输入 6 位数字...", _ => "Введите 6 цифр..." };
        public string AuthPasswordNew => CurrentLanguage switch { "EN" => "New Password:", "ZH" => "新密码:", _ => "Новый пароль:" };
        public string AuthPasswordConfirmLabel => CurrentLanguage switch { "EN" => "Confirm new password:", "ZH" => "确认新密码:", _ => "Подтвердите новый пароль:" };
        public string AuthPasswordConfirmPlaceholder => CurrentLanguage switch { "EN" => "Repeat new password...", "ZH" => "重复新密码...", _ => "Повторите новый пароль..." };
        public string AuthLoginBtn => CurrentLanguage switch { "EN" => "Log In", "ZH" => "立即登录", _ => "Войти" };
        public string AuthRegisterBtn => CurrentLanguage switch { "EN" => "Register", "ZH" => "立即注册", _ => "Зарегистрироваться" };
        public string AuthConfirmBtn => CurrentLanguage switch { "EN" => "Confirm", "ZH" => "确认", _ => "Подтвердить" };
        public string AuthSendResetBtn => CurrentLanguage switch { "EN" => "Send Reset Code", "ZH" => "发送重置码", _ => "Отправить код сброса" };
        public string AuthSavePasswordBtn => CurrentLanguage switch { "EN" => "Save New Password", "ZH" => "保存新密码", _ => "Сохранить новый пароль" };
        public string AuthBackBtn => CurrentLanguage switch { "EN" => "Back", "ZH" => "返回", _ => "Назад" };
        public string AuthErrRegisterFirst => CurrentLanguage switch { "EN" => "User must be registered first", "ZH" => "必须先注册用户", _ => "Сначала необходимо зарегистрировать пользователя" };
        public string AuthErrEnterUserEmail => CurrentLanguage switch { "EN" => "Please enter username or email", "ZH" => "请输入用户名或电子邮箱", _ => "Введите имя пользователя или Email" };
        public string AuthErrEnterPassword => CurrentLanguage switch { "EN" => "Please enter password", "ZH" => "请输入密码", _ => "Введите пароль" };
        public string AuthErrEnterUsername => CurrentLanguage switch { "EN" => "Please enter username", "ZH" => "请输入用户名", _ => "Введите имя пользователя" };
        public string AuthErrEnterEmail => CurrentLanguage switch { "EN" => "Please enter email", "ZH" => "请输入电子邮箱", _ => "Введите почту" };
        public string AuthErrInvalidEmail => CurrentLanguage switch { "EN" => "Please enter a valid email address.", "ZH" => "请输入有效的电子邮箱地址。", _ => "Пожалуйста, введите корректный Email." };
        public string AuthErrEnterCode => CurrentLanguage switch { "EN" => "Please enter verification code", "ZH" => "请输入验证码", _ => "Введите код подтверждения" };
        public string AuthErrPasswordsMismatch => CurrentLanguage switch { "EN" => "Passwords do not match", "ZH" => "密码不匹配", _ => "Пароли не совпадают" };

        // Cleaner (CleanerView)
        public string CleanerBrowserCache => CurrentLanguage switch { "EN" => "Browser Cache", "ZH" => "浏览器缓存", _ => "Кэш браузеров" };
        public string CleanerBrowserCacheDesc => CurrentLanguage switch { "EN" => "Temporary files of Chrome, Edge, Firefox", "ZH" => "Chrome、Edge、Firefox 临时文件", _ => "Временные файлы Chrome, Edge, Firefox" };

        // Dock (DockWindow)
        public string DockTooltipExplorer => CurrentLanguage switch { "EN" => "Explorer", "ZH" => "资源管理器", _ => "Проводник" };
        public string DockTooltipNotepad => CurrentLanguage switch { "EN" => "Notepad", "ZH" => "记事本", _ => "Блокнот" };
        public string DockTooltipCalculator => CurrentLanguage switch { "EN" => "Calculator", "ZH" => "计算器", _ => "Калькулятор" };
        public string DockTooltipBrowser => CurrentLanguage switch { "EN" => "Browser", "ZH" => "浏览器", _ => "Браузер" };
        public string DockTooltipYandexMusic => CurrentLanguage switch { "EN" => "Yandex Music", "ZH" => "Yandex 音乐", _ => "Яндекс.Музыка" };

        // Dynamic Island Window (DynamicIslandWindow & CS)
        public string DiScreenshotSaved => CurrentLanguage switch { "EN" => "Screenshot saved", "ZH" => "截图已保存", _ => "Снимок экрана сохранен" };
        public string DiCopy => CurrentLanguage switch { "EN" => "Copy", "ZH" => "复制", _ => "Копия" };
        public string DiSave => CurrentLanguage switch { "EN" => "Save", "ZH" => "保存", _ => "Сохранить" };
        public string DiFocusPrefix => CurrentLanguage switch { "EN" => "Focus: ", "ZH" => "专注: ", _ => "Фокус: " };
        public string DiOverheatAlert => CurrentLanguage switch { "EN" => "SYSTEM OVERHEAT!", "ZH" => "系统过热警告！", _ => "ПЕРЕГРЕВ СИСТЕМЫ!" };

        // Tools Additional Localization
        public string ToolsErrorCableNotFound => CurrentLanguage switch { "EN" => "Error: CABLE Input device not found.", "ZH" => "错误：未找到 CABLE Input 设备。", _ => "Ошибка: Устройство CABLE Input не найдено." };
        public string ToolsVolumeProfileGames => CurrentLanguage switch { "EN" => "Volume profile: Games (80%)", "ZH" => "音量模式: 游戏 (80%)", _ => "Профиль громкости: Игры (80%)" };
        public string ToolsVolumeProfileMovies => CurrentLanguage switch { "EN" => "Volume profile: Movies (60%)", "ZH" => "音量模式: 电影 (60%)", _ => "Профиль громкости: Фильмы (60%)" };
        public string ToolsVolumeProfileWork => CurrentLanguage switch { "EN" => "Volume profile: Work (20%)", "ZH" => "音量模式: 工作 (20%)", _ => "Профиль громкости: Работа (20%)" };
        public string ToolsPresetReset => CurrentLanguage switch { "EN" => "Reset", "ZH" => "重置", _ => "Сброс" };
        public string ToolsPresetRock => CurrentLanguage switch { "EN" => "Rock 🎸", "ZH" => "摇滚 🎸", _ => "Рок 🎸" };
        public string ToolsPresetPop => CurrentLanguage switch { "EN" => "Pop 🎤", "ZH" => "流行 🎤", _ => "Поп 🎤" };
        public string ToolsPresetBass => CurrentLanguage switch { "EN" => "Bass 🔊", "ZH" => "低音 🔊", _ => "Бас 🔊" };
    }
}


