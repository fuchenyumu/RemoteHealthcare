-- 远程会诊评价表（PostgreSQL 16）
-- 对应 99下一阶段20251222.md 中的“评价闭环落地”

BEGIN;

CREATE SEQUENCE IF NOT EXISTS purest_rc_cons_evaluation_seq START WITH 10000;

CREATE TABLE IF NOT EXISTS purest_rc_consultation_evaluation (
    id bigint NOT NULL DEFAULT nextval('purest_rc_cons_evaluation_seq'),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    consultation_id bigint NOT NULL,
    patient_name varchar(40),
    score integer NOT NULL DEFAULT 5, -- 评分 1-5
    tags varchar(200), -- 评价标签，逗号分隔，如：态度好,专业,解答清晰
    content text, -- 评价内容
    is_anonymous boolean DEFAULT false, -- 是否匿名
    CONSTRAINT pk_purest_rc_consultation_evaluation PRIMARY KEY (id),
    CONSTRAINT fk_rc_evaluation_consultation FOREIGN KEY (consultation_id) REFERENCES purest_rc_consultation (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_rc_consultation_evaluation IS '远程会诊患者评价表';
COMMENT ON COLUMN purest_rc_consultation_evaluation.score IS '评分 1-5';
COMMENT ON COLUMN purest_rc_consultation_evaluation.tags IS '评价标签，逗号分隔';
COMMENT ON COLUMN purest_rc_consultation_evaluation.content IS '评价内容';

CREATE INDEX IF NOT EXISTS idx_rc_evaluation_consult ON purest_rc_consultation_evaluation (consultation_id);

COMMIT;






