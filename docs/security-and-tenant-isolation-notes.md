# Vector Security and Tenant Isolation Notes

## Intended Login Flow

1. Splash screen
2. Company workspace login
3. User access/login layer
4. Role-based home page

The company workspace login identifies which client workspace or tenant the user is trying to access. It must not be treated as the only security boundary.

## Required Security Model

Vector must use layered access control:

- Company workspace identifier, such as a workspace code or subdomain.
- Senior management account login with strong password requirements.
- Multi-factor authentication for senior management and management roles.
- Role-based access for Staff, Operational Management, and Senior Management.
- Tenant isolation enforced server-side on every database query.
- Every client-owned record must include a CompanyId or TenantId.
- Staff access should be created or invited by authorised management users.
- Task-specific access must be limited to the assigned task and expire when configured.

## Password and Account Safety

Recommended production requirements:

- Minimum password length: 14 to 16 characters.
- Passphrases allowed.
- Passwords must be hashed, never stored as plain text.
- Account lockout after repeated failed login attempts.
- Verified password reset flow.
- MFA required for Senior Management.
- Optional SSO support later for larger clients.

## Data Protection

Vector should use:

- HTTPS for all traffic.
- Encrypted database and file storage at rest.
- Secure storage for connection strings and secrets.
- Least-privilege access for administrators.
- Audit logging for important actions.
- Secure file upload validation and storage.
- Backups and recovery controls.

## Tenant Separation Rule

No client must be able to access another client's data by changing a URL, button, browser state, workspace code, role selection, or request parameter.

Tenant separation must be enforced in backend logic and database queries, not only through the user interface.
