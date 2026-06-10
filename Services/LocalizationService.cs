using CommunityToolkit.Mvvm.ComponentModel;

namespace MacStyleHub.Services
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
            "ZH" => "Плееры не запущены",
            _ => "未检测到活动播放器"
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

        public string AboutFeatureLangTitle => CurrentLanguage switch
        {
            "EN" => "Personalization",
            "ZH" => "个性化",
            _ => "Персонализация"
        };

        public string AboutFeatureLangDesc => CurrentLanguage switch
        {
            "EN" => "Theme switching (Light/Dark) and multi-language support (RU, EN, ZH).",
            "ZH" => "深浅色主题切换和多语言支持 (RU, EN, ZH)。",
            _ => "Смена тем оформления (Светлая/Темная) и поддержка 3 языков (RU, EN, ZH)."
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
            _ => "Оперативная память (RAM)"
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
            "EN" => "Click the button to open Yandex Maps and set your location",
            "ZH" => "点击按钮打开 Yandex 地图并设置您的位置",
            _ => "Нажмите кнопку, чтобы открыть Яндекс.Карты и настроить ваше местоположение"
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
            "ZH" => "Веб-версия",
            _ => "Веб-версия"
        };

        public string PlayerSessionsToggle => CurrentLanguage switch
        {
            "EN" => "Show/Hide Active Sessions",
            "ZH" => "显示/隐藏活动会话",
            _ => "Свернуть/развернуть активные сессии"
        };
    }
}

