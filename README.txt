MyStore - Secure Authentication Phase

What changed
- Passwords are now stored using PBKDF2-SHA256 with per-password random salts.
- Existing accounts from the old demo database are migrated to password hashes automatically on their next successful login.
- Access JWT lifetime is 15 minutes.
- Refresh tokens are random, stored only as SHA-256 hashes in SQLite, and expire after 7 days.
- Refresh token rotation happens whenever /api/auth/refresh is called.
- Logout revokes the current refresh token.
- React automatically refreshes an expired access token once, then retries the original API request.
- Admin role is still enforced by the backend controllers/authorization attributes.
- Forgot-password no longer exposes a reset code in the API response. Connect the endpoint to a real email provider before production use.

Run
1. START-BACKEND.bat
2. START-FRONTEND.bat
3. Open http://localhost:5173

Demo admin
Email: admin@mystore.com
Password: 123456

Security note
The JWT signing key in appsettings.json is a development key. Before production, replace it with a long random secret stored in environment variables or a secret manager. Configure a real email provider for password resets and HTTPS for deployment.


Database upgrade note: this build checks SQLite table columns before ALTER TABLE, so existing PasswordHash/RefreshToken columns will not produce duplicate-column errors in the terminal.
