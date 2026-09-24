-- =====================================================
-- V20261005 — Chuẩn hoá mọi SĐT Việt Nam về MỘT dạng duy nhất: +84xxxxxxxxx.
--   * public.users.phone
--   * public.student_profiles.parent_phone
--   * recorder.students.parent_phone
-- Backend từ nay chuẩn hoá lúc lưu (PhoneNumberHelper.ToE164) và tra cứu khớp cả dạng cũ,
-- nên chạy migration này TRƯỚC hay SAU khi deploy đều an toàn.
--
-- ĐỂ Ở migrations/proposed/ — CHƯA nằm trong migrations/managed/ vì môi trường Development
-- tự chạy managed migrations lúc khởi động (và appsettings.Development đang trỏ Supabase).
-- Chạy kiểm tra trùng (bước 0) trước; sạch rồi mới chuyển file sang managed/.
--
-- users.phone có unique index (users_phone_key): nếu 2 tài khoản cùng một số nhưng viết khác
-- nhau (vd "0901234567" và "+84901234567"), migration DỪNG và không đổi gì — cần gộp / xử lý
-- tay các tài khoản đó trước.
-- =====================================================

-- Bước 0 (chạy riêng để xem trước, không đổi dữ liệu):
--   SELECT norm, array_agg(userid) AS user_ids, array_agg(phone) AS phones
--     FROM (SELECT userid, phone,
--                  CASE
--                    WHEN p ~ '^\+84\d{9,10}$' THEN p
--                    WHEN p ~ '^84\d{9,10}$'   THEN '+' || p
--                    WHEN p ~ '^0\d{9,10}$'    THEN '+84' || substr(p, 2)
--                    ELSE p
--                  END AS norm
--             FROM (SELECT userid, phone, regexp_replace(phone, '[\s.\-()]', '', 'g') AS p
--                     FROM users WHERE phone IS NOT NULL) x) y
--    GROUP BY norm HAVING count(*) > 1;

BEGIN;

CREATE OR REPLACE FUNCTION pg_temp.to_e164_vn(raw text) RETURNS text
LANGUAGE sql IMMUTABLE AS $$
    SELECT CASE
             WHEN raw IS NULL OR btrim(raw) = '' THEN NULL
             WHEN p ~ '^\+84\d{9,10}$' THEN p
             WHEN p ~ '^84\d{9,10}$'   THEN '+' || p
             WHEN p ~ '^0\d{9,10}$'    THEN '+84' || substr(p, 2)
             ELSE raw                          -- không nhận ra: giữ nguyên, không đoán
           END
      FROM (SELECT regexp_replace(raw, '[\s.\-()]', '', 'g') AS p) s
$$;

DO $$
DECLARE
    dup_count int;
BEGIN
    SELECT count(*) INTO dup_count FROM (
        SELECT pg_temp.to_e164_vn(phone) AS norm
          FROM users
         WHERE phone IS NOT NULL
         GROUP BY 1
        HAVING count(*) > 1
    ) d;
    IF dup_count > 0 THEN
        RAISE EXCEPTION 'normalize_phone_e164: % số điện thoại bị trùng sau chuẩn hoá — chạy truy vấn bước 0 để xem và xử lý trước.', dup_count;
    END IF;
END $$;

UPDATE users
   SET phone = pg_temp.to_e164_vn(phone)
 WHERE phone IS NOT NULL
   AND phone IS DISTINCT FROM pg_temp.to_e164_vn(phone);

UPDATE student_profiles
   SET parent_phone = pg_temp.to_e164_vn(parent_phone)
 WHERE parent_phone IS NOT NULL
   AND parent_phone IS DISTINCT FROM pg_temp.to_e164_vn(parent_phone);

UPDATE recorder.students
   SET parent_phone = pg_temp.to_e164_vn(parent_phone)
 WHERE parent_phone IS NOT NULL
   AND parent_phone IS DISTINCT FROM pg_temp.to_e164_vn(parent_phone);

COMMIT;
