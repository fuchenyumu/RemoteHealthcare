-- 会诊报告签署流程扩展表结构
-- 基于现有 purest_rc_business.sql 扩展
-- 执行顺序：在 purest_rc_business.sql 之后执行

BEGIN;

-- 1. 报告签署流程表
CREATE SEQUENCE IF NOT EXISTS purest_rc_report_sign_flow_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_report_sign_flow (
    id bigint NOT NULL DEFAULT nextval('purest_rc_report_sign_flow_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    report_id bigint NOT NULL,
    signer_id bigint NOT NULL,
    signer_role varchar(20) NOT NULL,  -- 主持人/专家/秘书
    sign_order integer NOT NULL,       -- 签署顺序
    sign_status varchar(20) NOT NULL,  -- 待签署/已签署/已拒绝/已取消
    sign_time timestamp(0),
    signature_file_id bigint,          -- 签名文件ID
    sign_ip varchar(50),               -- 签署IP
    sign_device varchar(100),          -- 签署设备信息
    CONSTRAINT pk_purest_rc_report_sign_flow PRIMARY KEY (id),
    CONSTRAINT fk_rc_sign_flow_report FOREIGN KEY (report_id) REFERENCES purest_rc_consultation_report (id) ON DELETE CASCADE,
    CONSTRAINT fk_rc_sign_flow_signer FOREIGN KEY (signer_id) REFERENCES purest_user (id),
    CONSTRAINT fk_rc_sign_flow_signature FOREIGN KEY (signature_file_id) REFERENCES purest_file_record (id)
);

COMMENT ON TABLE purest_rc_report_sign_flow IS '报告签署流程表';
COMMENT ON COLUMN purest_rc_report_sign_flow.signer_role IS '签署人角色字典：RC_SIGNER_ROLE';
COMMENT ON COLUMN purest_rc_report_sign_flow.sign_status IS '签署状态字典：RC_SIGN_STATUS';

CREATE INDEX IF NOT EXISTS idx_rc_sign_flow_report ON purest_rc_report_sign_flow (report_id);
CREATE INDEX IF NOT EXISTS idx_rc_sign_flow_signer ON purest_rc_report_sign_flow (signer_id);
CREATE INDEX IF NOT EXISTS idx_rc_sign_flow_status ON purest_rc_report_sign_flow (sign_status);

-- 2. 报告版本历史表
CREATE SEQUENCE IF NOT EXISTS purest_rc_report_version_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_report_version (
    id bigint NOT NULL DEFAULT nextval('purest_rc_report_version_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    report_id bigint NOT NULL,
    version_number integer NOT NULL,
    version_content jsonb NOT NULL,    -- 报告完整内容
    change_reason varchar(500),        -- 修改原因
    is_active boolean NOT NULL DEFAULT true,
    CONSTRAINT pk_purest_rc_report_version PRIMARY KEY (id),
    CONSTRAINT fk_rc_version_report FOREIGN KEY (report_id) REFERENCES purest_rc_consultation_report (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_rc_report_version IS '报告版本历史表';
COMMENT ON COLUMN purest_rc_report_version.version_content IS '存储报告的完整JSON数据，包括摘要、诊断、建议等';
COMMENT ON COLUMN purest_rc_report_version.is_active IS '是否为当前激活版本';

CREATE INDEX IF NOT EXISTS idx_rc_version_report ON purest_rc_report_version (report_id);
CREATE INDEX IF NOT EXISTS idx_rc_version_number ON purest_rc_report_version (version_number);

-- 3. 扩展字典配置
-- 新增签署相关字典分类
INSERT INTO purest_dict_category (id, create_by, create_time, remark, name, code)
VALUES
    (90013, 0, now(), '报告签署流程中的角色', '报告签署-角色', 'RC_SIGNER_ROLE'),
    (90014, 0, now(), '签署流程状态', '报告签署-状态', 'RC_SIGN_STATUS')
ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name, remark = EXCLUDED.remark;

-- 新增签署角色字典数据
INSERT INTO purest_dict_data (id, create_by, create_time, category_id, name, code, sort)
VALUES
    (911201, 0, now(), 90013, '主持人', 'HOST', 1),
    (911202, 0, now(), 90013, '专家', 'EXPERT', 2),
    (911203, 0, now(), 90013, '秘书', 'SECRETARY', 3)
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, sort = EXCLUDED.sort;

-- 新增签署状态字典数据
INSERT INTO purest_dict_data (id, create_by, create_time, category_id, name, code, sort)
VALUES
    (911301, 0, now(), 90014, '待签署', 'PENDING', 1),
    (911302, 0, now(), 90014, '已签署', 'SIGNED', 2),
    (911303, 0, now(), 90014, '已拒绝', 'REJECTED', 3),
    (911304, 0, now(), 90014, '已取消', 'CANCELLED', 4)
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, sort = EXCLUDED.sort;

-- 4. 更新现有报告表结构（如果需要）
-- 现有的 purest_rc_consultation_report 已经包含基本字段，这里添加额外字段（可选）
-- ALTER TABLE purest_rc_consultation_report ADD COLUMN IF NOT EXISTS current_version integer DEFAULT 1;

COMMIT;