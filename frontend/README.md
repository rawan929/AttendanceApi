# TapTrack frontend (Vue 3 + Vite + Pinia + Vue Router + axios + SignalR)

    npm install
    npm run dev

1. `.env` -> `VITE_API_URL=https://localhost:7123` (backend base URL, no `/api`).
2. Backend: add your dev URL (default http://localhost:5173) to `Cors:Origins` and run `dotnet dev-certs https --trust` once.
3. Default admin from the API guide: admin@attendance.local / Admin@12345. Passwords for new students/doctors need 8+ chars with a digit (Identity rules).

All HTTP lives in `src/api/*.api.js`; each file maps the backend DTOs to the shapes the views use. Errors are toasted centrally in `src/api/axios.js`.

## Known gaps to ask the backend team about
- Doctor needs: list of OWN courses, list of devices, and students of own courses (for linking pending cards). All are Admin-only today; the app derives courses/devices from the doctor's past sessions as a workaround.
- Course DTO has no enrolled count; tables fetch up to 100 rows and filter/page on the client.
