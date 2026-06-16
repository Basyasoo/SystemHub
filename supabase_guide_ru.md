# Инструкция по настройке Supabase для базы данных и отправки кодов на почту

[Supabase](https://supabase.com) — это открытая облачная альтернатива Firebase на базе СУБД PostgreSQL. В приложении SystemHub он используется для:
1. Хранения учетных записей пользователей, их имен, аватаров и даты регистрации.
2. Безопасной аутентификации (регистрация, вход, изменение пароля).
3. Автоматической генерации и отправки одноразовых кодов подтверждения (OTP) на электронную почту.

---

## Шаг 1. Создание проекта на Supabase
1. Перейдите на [supabase.com](https://supabase.com) и зарегистрируйтесь (можно войти через GitHub).
2. Создайте новый проект (кнопка **New Project**).
3. Укажите имя проекта (например, `SystemHub`), пароль для базы данных и выберите ближайший регион серверов.
4. Нажмите **Create New Project** и подождите несколько минут, пока база данных развернется.

## Шаг 2. Получение URL и API ключа
1. В личном кабинете Supabase перейдите в раздел **Project Settings** (иконка шестеренки снизу в левом боковом меню) -> **API**.
2. В секции **API Settings** найдите:
   - **Project URL** (ссылка вида `https://xxxx.supabase.co`).
   - **Project API keys** -> **anon / public** (длинная строка JWT-токена, начинающаяся на `eyJ...`).
3. Скопируйте оба этих значения.

## Шаг 3. Ввод настроек в SystemHub
Настройки подключения вынесены в локальный конфигурационный файл на вашем компьютере:
1. Запустите приложение SystemHub хотя бы один раз, чтобы оно автоматически сгенерировало структуру папок.
2. Перейдите по пути: `%APPDATA%\SystemHub` (вставьте этот путь в проводник Windows и нажмите Enter).
3. Найдите файл `supabase_config.json` и откройте его любым текстовым редактором (например, Блокнотом).
4. Замените шаблонные значения скопированными данными вашего проекта:
   ```json
   {
     "SupabaseUrl": "https://ваша-ссылка.supabase.co",
     "SupabaseKey": "ваш-anon-key-ключ"
   }
   ```
5. Сохраните файл и перезапустите приложение SystemHub.
6. Теперь авторизация, база данных и отправка кодов на почту будут работать через ваш проект!

---

## 🔑 Настройка входа по имени пользователя (Login)

Чтобы пользователи могли входить по своему никнейму (имени пользователя), в базе данных Supabase необходимо создать публичную таблицу `profiles` и настроить триггер, который будет автоматически связывать никнейм с почтой при регистрации.

### Инструкция:
1. В панели управления Supabase перейдите в раздел **SQL Editor** (иконка `SQL` в левом боковом меню).
2. Нажмите **New query** (Создать новый запрос).
3. Вставьте следующий SQL-код и нажмите кнопку **Run** (Запустить):

```sql
-- Создание таблицы публичных профилей
create table public.profiles (
  id uuid references auth.users on delete cascade not null primary key,
  username text unique not null,
  email text not null
);

-- Включение Row Level Security (RLS) для защиты данных
alter table public.profiles enable row level security;

-- Создание политик доступа: чтение разрешено всем, запись — только владельцу профиля
create policy "Public profiles are viewable by everyone." on public.profiles
  for select using (true);

create policy "Users can insert their own profile." on public.profiles
  for insert with check (auth.uid() = id);

create policy "Users can update their own profile." on public.profiles
  for update using (auth.uid() = id);

-- Функция для автоматического создания профиля при регистрации нового пользователя
create or replace function public.handle_new_user()
returns trigger as $$
begin
  insert into public.profiles (id, username, email)
  values (
    new.id,
    coalesce(new.raw_user_meta_data->>'username', new.email),
    new.email
  );
  return new;
end;
$$ language plpgsql security definer;

-- Триггер на создание пользователя
create or replace trigger on_auth_user_created
  after insert on auth.users
  for each row execute procedure public.handle_new_user();
```

После выполнения этого SQL-запроса все новые зарегистрированные пользователи автоматически попадут в таблицу `profiles`, что позволит приложению сопоставлять логин с почтой при авторизации.

---

## 📧 Как настроить отправку кодов подтверждения (Supabase Auth)

По умолчанию Supabase отправляет письма со ссылками подтверждения, но приложение ожидает **6-значный цифровой код (OTP)**. Также для защиты необходимо включить обязательное подтверждение почты.

### Шаг 1. Включение обязательного подтверждения почты
1. В панели Supabase перейдите в **Authentication** -> **Providers** -> **Email**.
2. Включите переключатель **Confirm email** (если он выключен, пользователи смогут входить без подтверждения, и письма отправляться не будут).
3. Нажмите **Save** (Сохранить).

### Шаг 2. Настройка шаблона писем с OTP кодами
1. Перейдите в **Authentication** -> **Email Templates**.
2. Выберите шаблон **Confirm Signup** (Подтверждение регистрации).
3. Отредактируйте текст письма, вставив в него шаблонный тег `{{ .Token }}` вместо `{{ .ConfirmationURL }}`.
   > Вы можете скопировать готовый HTML-код из файла `signup_email_template.html` в корне проекта и вставить его целиком в редактор шаблона в Supabase. Главное, чтобы там присутствовал тег `{{ .Token }}`.
4. Выберите шаблон **Reset Password** (Восстановление пароля) и вставьте в него код из файла `recovery_email_template.html` (или добавьте тег `{{ .Token }}`).
5. Сохраните изменения.

> [!TIP]
> Встроенный почтовый сервис Supabase имеет ограничение (3 письма в час). Для стабильной работы подключите собственный SMTP-сервер (например, Yandex, Google или Resend) в разделе **Authentication** -> **SMTP**.
