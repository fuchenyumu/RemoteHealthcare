-- 03.6_demo_users_setup.sql
-- 修复：统一使用 990... 系列 ID，确保与业务种子数据关联

BEGIN;

-- 1. 确保 demo_host 绑定了正确的演示角色 (9900000200001 - 主持人)
INSERT INTO "purest_user_role" ("id", "create_by", "create_time", "user_id", "role_id")
VALUES (9900000400001, 0, now(), 9900000100001, 9900000200001)
ON CONFLICT (id) DO UPDATE SET 
    user_id = EXCLUDED.user_id,
    role_id = EXCLUDED.role_id;

-- 2. 确保 demo_expert 绑定了专家角色 (9900000200002)
INSERT INTO "purest_user_role" ("id", "create_by", "create_time", "user_id", "role_id")
VALUES (9900000400002, 0, now(), 9900000100002, 9900000200002)
ON CONFLICT (id) DO UPDATE SET 
    user_id = EXCLUDED.user_id,
    role_id = EXCLUDED.role_id;

COMMIT;
