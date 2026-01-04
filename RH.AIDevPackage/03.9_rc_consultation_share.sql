-- 远程会诊分享链接表（PostgreSQL 16）
-- 对应会诊分享链接功能，支持第三方用户无需登录参与会诊

BEGIN;

-- 创建序列
CREATE SEQUENCE IF NOT EXISTS purest_rc_consultation_share_seq START WITH 10000;

-- 创建分享链接表
CREATE TABLE IF NOT EXISTS purest_rc_consultation_share (
    id bigint NOT NULL DEFAULT nextval('purest_rc_consultation_share_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    consultation_id bigint NOT NULL,
    share_token varchar(128) NOT NULL,
    creator_user_id bigint NOT NULL,
    visitor_display_name varchar(100),
    expire_time timestamp(0) NOT NULL,
    max_usage_count integer NOT NULL DEFAULT 0,
    used_count integer NOT NULL DEFAULT 0,
    is_enabled boolean DEFAULT true,
    first_access_time timestamp(0),
    last_access_time timestamp(0),
    CONSTRAINT pk_purest_rc_consultation_share PRIMARY KEY (id),
    CONSTRAINT uk_rc_share_token UNIQUE (share_token),
    CONSTRAINT fk_rc_share_consultation FOREIGN KEY (consultation_id) REFERENCES purest_rc_consultation (id) ON DELETE CASCADE
);

-- 表注释
COMMENT ON TABLE purest_rc_consultation_share IS '远程会诊分享链接表';

-- 列注释
COMMENT ON COLUMN purest_rc_consultation_share.consultation_id IS '会诊ID';
COMMENT ON COLUMN purest_rc_consultation_share.share_token IS '分享码(唯一标识)';
COMMENT ON COLUMN purest_rc_consultation_share.creator_user_id IS '创建人用户ID';
COMMENT ON COLUMN purest_rc_consultation_share.visitor_display_name IS '访客默认显示名称';
COMMENT ON COLUMN purest_rc_consultation_share.expire_time IS '过期时间';
COMMENT ON COLUMN purest_rc_consultation_share.max_usage_count IS '最大使用次数(0表示不限制)';
COMMENT ON COLUMN purest_rc_consultation_share.used_count IS '已使用次数';
COMMENT ON COLUMN purest_rc_consultation_share.is_enabled IS '是否启用';
COMMENT ON COLUMN purest_rc_consultation_share.first_access_time IS '首次访问时间';
COMMENT ON COLUMN purest_rc_consultation_share.last_access_time IS '最后访问时间';

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_rc_share_consultation ON purest_rc_consultation_share (consultation_id);
CREATE INDEX IF NOT EXISTS idx_rc_share_creator ON purest_rc_consultation_share (creator_user_id);
CREATE INDEX IF NOT EXISTS idx_rc_share_expire ON purest_rc_consultation_share (expire_time);
CREATE INDEX IF NOT EXISTS idx_rc_share_create_time ON purest_rc_consultation_share (create_time);

-- 复合索引用于常用查询
CREATE INDEX IF NOT EXISTS idx_rc_share_enabled_expire ON purest_rc_consultation_share (is_enabled, expire_time);

COMMIT;
