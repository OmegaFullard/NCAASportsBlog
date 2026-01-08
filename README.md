# NCAA Sports Blog

A full-stack Django-based blog and news platform focused on NCAA sports — scores, schedules, team news, in-depth analysis, and community-driven content. This repository uses Django REST Framework for the backend and Django templates (with optional React components) for the frontend.

Status: Draft — update configuration values and secrets before deploying.

Badges
- CI: (add your CI badge)
- Coverage: (add coverage badge)
- License: MIT

Table of Contents
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Environment variables](#environment-variables)
  - [Database setup & migrations](#database-setup--migrations)
  - [Run (dev & prod)](#run-dev--prod)
- [Testing](#testing)
- [API Integrations](#api-integrations)
- [Media & Storage](#media--storage)
- [Development workflow](#development-workflow)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgements](#acknowledgements)

Features
- Article publishing with rich-text (authors, editors, admin roles)
- Team pages: roster, schedule, results
- Periodic syncing of game schedules and scores from a sports data API
- REST API endpoints for clients (mobile, SPA)
- User accounts, authentication (JWT), and admin interface
- Comments (optional moderation)
- RSS feeds and social preview metadata
- Responsive UI (mobile / desktop)

Tech Stack (exact)
- Frontend: Django templates with optional React components (served via Django or a separate build)
- Backend: Python 3.10+, Django 4.x, Django REST Framework
- Database: PostgreSQL (production), SQLite for quick local development
- ORM / Migrations: Django ORM + migrations
- Authentication: Django built-in for admin + djangorestframework-simplejwt (JWT) for API
- Tests: pytest + pytest-django (unit/integration), Django test runner compatibility
- External sports API: CollegeFootballData (CFBD) — env var: CFBD_API_KEY
- Media: AWS S3 (recommended) / Local file storage for dev
- Optional: Celery + Redis for background tasks (syncing scores, sending emails)
- Hosting: Railway / Heroku / Render (example instructions below)
- License: MIT

Getting Started

Prerequisites
- Python 3.10+
- pip
- PostgreSQL (or use Railway/managed DB)
- Git
- (Optional) Redis for Celery
- Node & npm if you plan to build local React components / asset pipeline

Installation (local / development)
1. Clone the repo
   git clone https://github.com/OmegaFullard/NCAASportsBlog.git
   cd NCAASportsBlog

2. Create and activate a virtual environment
   python -m venv .venv
   source .venv/bin/activate   # macOS/Linux
   .venv\Scripts\activate      # Windows (PowerShell)

3. Install Python dependencies
   pip install -r requirements.txt

4. (Optional) Install Node packages (if front-end build is included)
   cd frontend && npm install

Environment variables
Create a `.env` file in the project root (do NOT commit this). Example variables:

- SECRET_KEY=your-django-secret-key
- DEBUG=True
- DJANGO_ALLOWED_HOSTS=localhost,127.0.0.1
- DATABASE_URL=postgres://USER:PASS@HOST:PORT/DBNAME
- CFBD_API_KEY=your-collegefootballdata-key
- AWS_ACCESS_KEY_ID=your-aws-key
- AWS_SECRET_ACCESS_KEY=your-aws-secret
- AWS_STORAGE_BUCKET_NAME=your-bucket
- REDIS_URL=redis://localhost:6379/0
- EMAIL_* (SMTP configuration for transactional email)
- CELERY_BROKER_URL=${REDIS_URL}

Tip: Use python-decouple or django-environ to load env vars into settings.py (repo may already include an env loader).

Database setup & migrations
- Create the DB (Postgres) or use local SQLite for quick dev.
- Run migrations:
  python manage.py migrate

- Create a superuser:
  python manage.py createsuperuser

Seed / demo data
- If repository includes a fixtures or management commands, load them:
  python manage.py loaddata initial_data.json
- Or use custom management commands to fetch initial sports data.

Run (dev & prod)
Development
- Run the Django dev server:
  python manage.py runserver

- If using a separate frontend build for React components:
  cd frontend
  npm run dev
  (Adjust ports / proxy settings as needed.)

Production
- Collect static files:
  python manage.py collectstatic --noinput

- Start with Gunicorn (example):
  gunicorn config.wsgi:application --bind 0.0.0.0:$PORT

- If using Docker, build & run container per Dockerfile (if present).

Testing

Unit & integration tests (pytest)
- Run all tests:
  pytest

- Run Django test suite:
  python manage.py test

Example CI command (GitHub Actions):
- pip install -r requirements.txt
- python -m pytest --maxfail=1 --disable-warnings -q

End-to-end (optional)
- Cypress / Playwright commands (if configured) — example:
  npm run test:e2e

API Integrations (CollegeFootballData)
We reference CollegeFootballData (CFBD) to populate schedules, scores, and team metadata.

- Sign up for an API key at https://collegefootballdata.com/
- Set CFBD_API_KEY in `.env`

Example usage in code (high-level)
- Periodically call CFBD endpoints (games, rosters, teams)
- Map CFBD responses to local models (Team, Game, Player)
- Schedule updates via Celery beat or a cron job

Notes on usage & rate limits
- Respect the API's rate limits and attribution requirements specified by the provider.
- Cache responses and backfill gradually to avoid heavy bursts.

Media & Storage
- Development: use Django's default FileSystemStorage (MEDIA_ROOT)
- Production (recommended): use django-storages with AWS S3
  - Install: pip install boto3 django-storages
  - Configure AWS_* env vars and DEFAULT_FILE_STORAGE = 'storages.backends.s3boto3.S3Boto3Storage'

Background tasks & periodic sync
- Recommended: Celery + Redis
- Example commands:
  - Start worker: celery -A config worker -l info
  - Start beat: celery -A config beat -l info

Sample management command to sync scores (run via cron or Celery beat)
- python manage.py sync_scores --source=cfbd --season=2025

Development workflow
- Branching: feature branches off main, PRs into main
- Linting & formatting: black, isort, flake8 (configure in repo)
- Pre-commit hooks: recommended (pre-commit)

Deployment (example: Railway / Heroku)
- Ensure environment variables are set in the platform dashboard.
- Use a managed Postgres add-on.
- Configure static file serving (WhiteNoise for simple setups or S3 + CloudFront).
- Recommended Procfile (Heroku example):
  web: gunicorn config.wsgi --log-file -

- For Railway: connect repo, set env vars, add Postgres plugin, deploy.

Security
- Never commit `.env` or secrets.
- Rotate API keys and set minimal-scoped credentials for cloud services.
- Enable HTTPS in production and set SECURE_SSL_REDIRECT, SESSION_COOKIE_SECURE, CSRF_COOKIE_SECURE.

Contributing
- Fork the repo
- Create a branch: git checkout -b feat/your-feature
- Run tests and linters before committing
- Open a PR with a clear description and link to any relevant issue

Suggested PR checklist
- [ ] Tests added/updated
- [ ] Linting passed (black / flake8)
- [ ] Database migrations included (if required)
- [ ] Documentation updated (README / docs)

License
This project is MIT licensed — see LICENSE file.

Acknowledgements
- CollegeFootballData contributors and API
- Django, Django REST Framework, and the open-source community
```
````
