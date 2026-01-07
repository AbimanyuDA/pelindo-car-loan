-- Update all user passwords to BCrypt hash of "Password123!"
-- Hash: $2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy

UPDATE users SET password_hash = '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';

COMMIT;

SELECT username, email, role, SUBSTR(password_hash, 1, 20) AS hash_preview FROM users ORDER BY user_id;
