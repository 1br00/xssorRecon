# Xss0r SaaS Blazor App

## Implemented modules

- Auth (`/api/auth/register`, `/api/auth/login`)
- User dashboard (`/api/dashboard/me`)
- API key rotation (`/api/apikeys/rotate`)
- Coupons (`/api/coupons/redeem`)
- Downloads with signed links (`/api/downloads/*`)
- Admin endpoints (`/api/admin/*`)

## UI Routes

- `/` overview
- `/register`
- `/login`
- `/dashboard`
- `/pricing`
- `/downloads`
- `/admin`

## Notes

- Data is bootstrapped through `EnsureCreated` and seeding in `SeedData`.
- This is a production-oriented starter. You should add:
  - refresh tokens
  - email verification/forgot-password flow
  - payment provider integration
  - EF migrations and CI pipelines
