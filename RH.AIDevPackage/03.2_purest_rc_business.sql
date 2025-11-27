-- 远程会诊业务表结构（PostgreSQL 16）
-- 执行顺序：先执行 admin-postgresql.sql，再执行本脚本

BEGIN;

-- 1. 患者快照与打包
CREATE SEQUENCE IF NOT EXISTS purest_rc_patient_case_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_patient_case (
    id bigint NOT NULL DEFAULT nextval('purest_rc_patient_case_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    his_patient_id varchar(50),
    his_visit_id varchar(50),
    patient_name varchar(40) NOT NULL,
    patient_sex varchar(2) NOT NULL,
    patient_birth date,
    patient_type varchar(20) NOT NULL,
    id_card varchar(64),
    phone varchar(30),
    contact_address varchar(200),
    medical_summary text,
    allergies text,
    gestational_weeks numeric(4,1),
    child_age_months integer,
    data_source varchar(30) NOT NULL,
    CONSTRAINT pk_purest_rc_patient_case PRIMARY KEY (id)
);

COMMENT ON TABLE purest_rc_patient_case IS '远程会诊患者快照（同步院内主数据）';

CREATE SEQUENCE IF NOT EXISTS purest_rc_patient_pack_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_patient_pack (
    id bigint NOT NULL DEFAULT nextval('purest_rc_patient_pack_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    case_id bigint NOT NULL,
    pack_no varchar(30) NOT NULL,
    pack_status varchar(20) NOT NULL,
    expire_time timestamp(0),
    file_count integer NOT NULL DEFAULT 0,
    total_size bigint NOT NULL DEFAULT 0,
    sync_task_id bigint,
    CONSTRAINT pk_purest_rc_patient_pack PRIMARY KEY (id),
    CONSTRAINT fk_rc_pack_case FOREIGN KEY (case_id) REFERENCES purest_rc_patient_case (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_rc_pack_case ON purest_rc_patient_pack (case_id);

CREATE SEQUENCE IF NOT EXISTS purest_rc_patient_pack_file_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_patient_pack_file (
    id bigint NOT NULL DEFAULT nextval('purest_rc_patient_pack_file_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    pack_id bigint NOT NULL,
    file_id bigint NOT NULL,
    file_name varchar(255) NOT NULL,
    file_size bigint NOT NULL,
    CONSTRAINT pk_purest_rc_patient_pack_file PRIMARY KEY (id),
    CONSTRAINT fk_rc_pack_file_pack FOREIGN KEY (pack_id) REFERENCES purest_rc_patient_pack (id) ON DELETE CASCADE,
    CONSTRAINT fk_rc_pack_file_file FOREIGN KEY (file_id) REFERENCES purest_file_record (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_rc_pack_file_pack ON purest_rc_patient_pack_file (pack_id);

-- 2. 会诊主流程
CREATE SEQUENCE IF NOT EXISTS purest_rc_consultation_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_consultation (
    id bigint NOT NULL DEFAULT nextval('purest_rc_consultation_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    case_id bigint NOT NULL,
    pack_id bigint,
    apply_org_id bigint NOT NULL,
    apply_doctor_id bigint NOT NULL,
    target_org_id bigint,
    target_department varchar(50),
    target_expert_id bigint,
    purpose text NOT NULL,
    emergency_level varchar(20) NOT NULL,
    consultation_status varchar(20) NOT NULL,
    desired_start_time timestamp(0),
    desired_end_time timestamp(0),
    scheduled_start_time timestamp(0),
    scheduled_end_time timestamp(0),
    meeting_room_no varchar(50),
    rtc_channel_id varchar(64),
    rtc_vendor varchar(30),
    audit_doctor_id bigint,
    audit_time timestamp(0),
    audit_comment varchar(500),
    close_reason varchar(500),
    CONSTRAINT pk_purest_rc_consultation PRIMARY KEY (id),
    CONSTRAINT fk_rc_consultation_case FOREIGN KEY (case_id) REFERENCES purest_rc_patient_case (id),
    CONSTRAINT fk_rc_consultation_pack FOREIGN KEY (pack_id) REFERENCES purest_rc_patient_pack (id)
);

CREATE INDEX IF NOT EXISTS idx_rc_consult_status ON purest_rc_consultation (consultation_status);
CREATE INDEX IF NOT EXISTS idx_rc_consult_org ON purest_rc_consultation (apply_org_id, target_org_id);
CREATE INDEX IF NOT EXISTS idx_rc_consult_doctor ON purest_rc_consultation (apply_doctor_id);

-- 3. 参与成员
CREATE SEQUENCE IF NOT EXISTS purest_rc_cons_member_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_consultation_member (
    id bigint NOT NULL DEFAULT nextval('purest_rc_cons_member_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    consultation_id bigint NOT NULL,
    user_id bigint NOT NULL,
    org_id bigint NOT NULL,
    role_code varchar(20) NOT NULL,
    join_status varchar(20) NOT NULL,
    join_time timestamp(0),
    leave_time timestamp(0),
    CONSTRAINT pk_purest_rc_consultation_member PRIMARY KEY (id),
    CONSTRAINT fk_rc_member_consultation FOREIGN KEY (consultation_id) REFERENCES purest_rc_consultation (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_rc_member_consult ON purest_rc_consultation_member (consultation_id);
CREATE INDEX IF NOT EXISTS idx_rc_member_user ON purest_rc_consultation_member (user_id);

-- 4. 资料附件
CREATE SEQUENCE IF NOT EXISTS purest_rc_cons_attachment_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_consultation_attachment (
    id bigint NOT NULL DEFAULT nextval('purest_rc_cons_attachment_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    consultation_id bigint NOT NULL,
    file_id bigint,
    external_uri varchar(255),
    attachment_type varchar(20) NOT NULL,
    source_system varchar(30) NOT NULL,
    description varchar(200),
    CONSTRAINT pk_purest_rc_consultation_attachment PRIMARY KEY (id),
    CONSTRAINT fk_rc_attachment_consultation FOREIGN KEY (consultation_id) REFERENCES purest_rc_consultation (id) ON DELETE CASCADE,
    CONSTRAINT fk_rc_attachment_file FOREIGN KEY (file_id) REFERENCES purest_file_record (id)
);

CREATE INDEX IF NOT EXISTS idx_rc_attachment_consult ON purest_rc_consultation_attachment (consultation_id);
CREATE INDEX IF NOT EXISTS idx_rc_attachment_type ON purest_rc_consultation_attachment (attachment_type);

-- 5. 流程轨迹
CREATE SEQUENCE IF NOT EXISTS purest_rc_cons_timeline_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_consultation_timeline (
    id bigint NOT NULL DEFAULT nextval('purest_rc_cons_timeline_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    consultation_id bigint NOT NULL,
    event_code varchar(30) NOT NULL,
    event_content varchar(500),
    snapshot jsonb,
    CONSTRAINT pk_purest_rc_consultation_timeline PRIMARY KEY (id),
    CONSTRAINT fk_rc_timeline_consultation FOREIGN KEY (consultation_id) REFERENCES purest_rc_consultation (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_rc_timeline_event ON purest_rc_consultation_timeline (event_code);

-- 6. 会诊报告
CREATE SEQUENCE IF NOT EXISTS purest_rc_cons_report_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_consultation_report (
    id bigint NOT NULL DEFAULT nextval('purest_rc_cons_report_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    consultation_id bigint NOT NULL,
    summary text NOT NULL,
    diagnosis text,
    treatment_advice text,
    follow_up_plan text,
    signer_id bigint,
    signed_time timestamp(0),
    signature_file_id bigint,
    report_status varchar(20) NOT NULL,
    CONSTRAINT pk_purest_rc_consultation_report PRIMARY KEY (id),
    CONSTRAINT fk_rc_report_consultation FOREIGN KEY (consultation_id) REFERENCES purest_rc_consultation (id) ON DELETE CASCADE,
    CONSTRAINT fk_rc_report_signer FOREIGN KEY (signer_id) REFERENCES purest_user (id),
    CONSTRAINT fk_rc_report_signature_file FOREIGN KEY (signature_file_id) REFERENCES purest_file_record (id)
);

CREATE INDEX IF NOT EXISTS idx_rc_report_consult ON purest_rc_consultation_report (consultation_id);
CREATE INDEX IF NOT EXISTS idx_rc_report_status ON purest_rc_consultation_report (report_status);

-- 7. 数据同步任务
CREATE SEQUENCE IF NOT EXISTS purest_rc_sync_task_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_sync_task (
    id bigint NOT NULL DEFAULT nextval('purest_rc_sync_task_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    task_no varchar(40) NOT NULL,
    task_type varchar(20) NOT NULL,
    task_status varchar(20) NOT NULL,
    request_payload jsonb,
    response_payload jsonb,
    completed_time timestamp(0),
    retry_count integer NOT NULL DEFAULT 0,
    CONSTRAINT pk_purest_rc_sync_task PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS uk_rc_sync_task_no ON purest_rc_sync_task (task_no);

-- 8. 字典分类与数据
-- 类别数据从90001开始，避免与平台默认ID冲突
INSERT INTO purest_dict_category (id, create_by, create_time, update_by, update_time, remark, name, code)
VALUES
    (90001, 0, now(), NULL, NULL, '远程会诊患者类型', '远程会诊-患者类型', 'RC_PATIENT_TYPE'),
    (90002, 0, now(), NULL, NULL, '资料打包状态', '远程会诊-打包状态', 'RC_PACK_STATUS'),
    (90003, 0, now(), NULL, NULL, '会诊流程状态', '远程会诊-会诊状态', 'RC_CONSULT_STATUS'),
    (90004, 0, now(), NULL, NULL, '紧急程度', '远程会诊-紧急程度', 'RC_EMERGENCY_LEVEL'),
    (90005, 0, now(), NULL, NULL, '会诊成员角色', '远程会诊-成员角色', 'RC_MEMBER_ROLE'),
    (90006, 0, now(), NULL, NULL, '会诊成员状态', '远程会诊-成员状态', 'RC_MEMBER_STATUS'),
    (90007, 0, now(), NULL, NULL, '资料类型', '远程会诊-附件类型', 'RC_ATTACHMENT_TYPE'),
    (90008, 0, now(), NULL, NULL, '数据来源系统', '远程会诊-来源系统', 'RC_SOURCE_SYSTEM'),
    (90009, 0, now(), NULL, NULL, '流程事件', '远程会诊-事件', 'RC_TIMELINE_EVENT'),
    (90010, 0, now(), NULL, NULL, '会诊报告状态', '远程会诊-报告状态', 'RC_REPORT_STATUS'),
    (90011, 0, now(), NULL, NULL, '同步任务类型', '远程会诊-同步类型', 'RC_SYNC_TYPE'),
    (90012, 0, now(), NULL, NULL, '同步任务状态', '远程会诊-同步状态', 'RC_SYNC_STATUS')
ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name, remark = EXCLUDED.remark;

INSERT INTO purest_dict_data (id, create_by, create_time, update_by, update_time, remark, category_id, name, code, sort)
VALUES
    (910001, 0, now(), NULL, NULL, NULL, 90001, '孕妇', 'PREGNANT', 1),
    (910002, 0, now(), NULL, NULL, NULL, 90001, '儿童', 'CHILD', 2),
    (910003, 0, now(), NULL, NULL, NULL, 90001, '其他', 'OTHER', 3),

    (910101, 0, now(), NULL, NULL, NULL, 90002, '待打包', 'PENDING', 1),
    (910102, 0, now(), NULL, NULL, NULL, 90002, '打包中', 'PROCESSING', 2),
    (910103, 0, now(), NULL, NULL, NULL, 90002, '已完成', 'COMPLETED', 3),
    (910104, 0, now(), NULL, NULL, NULL, 90002, '已过期', 'EXPIRED', 4),

    (910201, 0, now(), NULL, NULL, NULL, 90003, '草稿', 'DRAFT', 1),
    (910202, 0, now(), NULL, NULL, NULL, 90003, '待审核', 'PENDING_REVIEW', 2),
    (910203, 0, now(), NULL, NULL, NULL, 90003, '驳回', 'REJECTED', 3),
    (910204, 0, now(), NULL, NULL, NULL, 90003, '待排期', 'WAIT_SCHEDULE', 4),
    (910205, 0, now(), NULL, NULL, NULL, 90003, '已排期', 'SCHEDULED', 5),
    (910206, 0, now(), NULL, NULL, NULL, 90003, '会诊中', 'IN_PROGRESS', 6),
    (910207, 0, now(), NULL, NULL, NULL, 90003, '已完成', 'FINISHED', 7),
    (910208, 0, now(), NULL, NULL, NULL, 90003, '已关闭', 'CLOSED', 8),

    (910301, 0, now(), NULL, NULL, NULL, 90004, '普通', 'NORMAL', 1),
    (910302, 0, now(), NULL, NULL, NULL, 90004, '紧急', 'URGENT', 2),
    (910303, 0, now(), NULL, NULL, NULL, 90004, '特急', 'CRITICAL', 3),

    (910401, 0, now(), NULL, NULL, NULL, 90005, '主持人', 'HOST', 1),
    (910402, 0, now(), NULL, NULL, NULL, 90005, '专家', 'EXPERT', 2),
    (910403, 0, now(), NULL, NULL, NULL, 90005, '观察员', 'OBSERVER', 3),
    (910404, 0, now(), NULL, NULL, NULL, 90005, '秘书', 'SECRETARY', 4),

    (910501, 0, now(), NULL, NULL, NULL, 90006, '待确认', 'PENDING', 1),
    (910502, 0, now(), NULL, NULL, NULL, 90006, '已确认', 'CONFIRMED', 2),
    (910503, 0, now(), NULL, NULL, NULL, 90006, '会诊中', 'IN_PROGRESS', 3),
    (910504, 0, now(), NULL, NULL, NULL, 90006, '已退出', 'EXITED', 4),

    (910601, 0, now(), NULL, NULL, NULL, 90007, '病历资料', 'EMR', 1),
    (910602, 0, now(), NULL, NULL, NULL, 90007, '检验报告', 'LIS', 2),
    (910603, 0, now(), NULL, NULL, NULL, 90007, '影像资料', 'PACS', 3),
    (910604, 0, now(), NULL, NULL, NULL, 90007, '其他附件', 'OTHER', 4),

    (910701, 0, now(), NULL, NULL, NULL, 90008, 'HIS系统', 'HIS', 1),
    (910702, 0, now(), NULL, NULL, NULL, 90008, 'LIS系统', 'LIS', 2),
    (910703, 0, now(), NULL, NULL, NULL, 90008, 'PACS系统', 'PACS', 3),
    (910704, 0, now(), NULL, NULL, NULL, 90008, 'EMR系统', 'EMR', 4),
    (910705, 0, now(), NULL, NULL, NULL, 90008, '手工上传', 'MANUAL', 5),

    (910801, 0, now(), NULL, NULL, NULL, 90009, '提交申请', 'SUBMITTED', 1),
    (910802, 0, now(), NULL, NULL, NULL, 90009, '审核通过', 'APPROVED', 2),
    (910803, 0, now(), NULL, NULL, NULL, 90009, '审核驳回', 'REJECTED', 3),
    (910804, 0, now(), NULL, NULL, NULL, 90009, '排期完成', 'SCHEDULED', 4),
    (910805, 0, now(), NULL, NULL, NULL, 90009, '会诊开始', 'STARTED', 5),
    (910806, 0, now(), NULL, NULL, NULL, 90009, '会诊结束', 'ENDED', 6),
    (910807, 0, now(), NULL, NULL, NULL, 90009, '报告签署', 'SIGNED', 7),
    (910808, 0, now(), NULL, NULL, NULL, 90009, '关闭申请', 'CLOSED', 8),

    (910901, 0, now(), NULL, NULL, NULL, 90010, '草稿', 'DRAFT', 1),
    (910902, 0, now(), NULL, NULL, NULL, 90010, '签署中', 'SIGNING', 2),
    (910903, 0, now(), NULL, NULL, NULL, 90010, '已签署', 'SIGNED', 3),
    (910904, 0, now(), NULL, NULL, NULL, 90010, '已归档', 'ARCHIVED', 4),

    (911001, 0, now(), NULL, NULL, NULL, 90011, '患者资料同步', 'PATIENT', 1),
    (911002, 0, now(), NULL, NULL, NULL, 90011, '检验报告同步', 'LAB', 2),
    (911003, 0, now(), NULL, NULL, NULL, 90011, '影像资料同步', 'IMAGE', 3),
    (911004, 0, now(), NULL, NULL, NULL, 90011, '会诊报告回传', 'REPORT', 4),

    (911101, 0, now(), NULL, NULL, NULL, 90012, '待执行', 'PENDING', 1),
    (911102, 0, now(), NULL, NULL, NULL, 90012, '执行中', 'PROCESSING', 2),
    (911103, 0, now(), NULL, NULL, NULL, 90012, '成功', 'SUCCESS', 3),
    (911104, 0, now(), NULL, NULL, NULL, 90012, '失败', 'FAILED', 4)
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, remark = EXCLUDED.remark, code = EXCLUDED.code, sort = EXCLUDED.sort, category_id = EXCLUDED.category_id;

COMMIT;

