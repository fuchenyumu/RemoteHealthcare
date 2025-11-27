-- PostgreSQL 16 schema converted from relationship-model/admin-mysql.sql
-- Date: 2025-11-06

BEGIN;

DROP TABLE IF EXISTS purest_workflow_instance CASCADE;
DROP TABLE IF EXISTS purest_wf_waiting_pointer CASCADE;
DROP TABLE IF EXISTS purest_wf_subscription CASCADE;
DROP TABLE IF EXISTS purest_wf_scheduled_command CASCADE;
DROP TABLE IF EXISTS purest_wf_execution_attribute CASCADE;
DROP TABLE IF EXISTS purest_wf_execution_pointer CASCADE;
DROP TABLE IF EXISTS purest_wf_execution_error CASCADE;
DROP TABLE IF EXISTS purest_wf_event CASCADE;
DROP TABLE IF EXISTS purest_wf_definition CASCADE;
DROP TABLE IF EXISTS purest_wf_auditing_record CASCADE;
DROP TABLE IF EXISTS purest_wf_workflow CASCADE;
DROP TABLE IF EXISTS purest_user_role CASCADE;
DROP TABLE IF EXISTS purest_user CASCADE;
DROP TABLE IF EXISTS purest_system_config CASCADE;
DROP TABLE IF EXISTS purest_role_function CASCADE;
DROP TABLE IF EXISTS purest_role CASCADE;
DROP TABLE IF EXISTS purest_request_log CASCADE;
DROP TABLE IF EXISTS purest_profile_system CASCADE;
DROP TABLE IF EXISTS purest_oauth2_user CASCADE;
DROP TABLE IF EXISTS purest_notice_record CASCADE;
DROP TABLE IF EXISTS purest_notice CASCADE;
DROP TABLE IF EXISTS purest_function_interface CASCADE;
DROP TABLE IF EXISTS purest_interface CASCADE;
DROP TABLE IF EXISTS purest_interface_group CASCADE;
DROP TABLE IF EXISTS purest_function CASCADE;
DROP TABLE IF EXISTS purest_file_record CASCADE;
DROP TABLE IF EXISTS purest_dict_data CASCADE;
DROP TABLE IF EXISTS purest_dict_category CASCADE;
DROP TABLE IF EXISTS purest_background_job_record CASCADE;
DROP TABLE IF EXISTS purest_organization CASCADE;

CREATE TABLE purest_background_job_record (
    id varchar(40) NOT NULL,
    job_name varchar(128) NOT NULL,
    job_args text NOT NULL,
    try_count numeric(10, 0),
    creation_time timestamp(0),
    next_try_time timestamp(0),
    last_try_time timestamp(0),
    is_abandoned boolean,
    priority numeric(10, 0),
    CONSTRAINT pk_purest_background_job_record PRIMARY KEY (id)
);

COMMENT ON TABLE purest_background_job_record IS '后台作业记录表';
COMMENT ON COLUMN purest_background_job_record.id IS 'Id';
COMMENT ON COLUMN purest_background_job_record.job_name IS '名称';
COMMENT ON COLUMN purest_background_job_record.job_args IS '参数';
COMMENT ON COLUMN purest_background_job_record.try_count IS '重试次数';
COMMENT ON COLUMN purest_background_job_record.creation_time IS '创建时间';
COMMENT ON COLUMN purest_background_job_record.next_try_time IS '下次执行时间';
COMMENT ON COLUMN purest_background_job_record.last_try_time IS '最后执行时间';
COMMENT ON COLUMN purest_background_job_record.is_abandoned IS '是否超时';
COMMENT ON COLUMN purest_background_job_record.priority IS '优先级';

CREATE TABLE purest_dict_category (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20) NOT NULL,
    code varchar(40) NOT NULL,
    CONSTRAINT pk_purest_dict_category PRIMARY KEY (id),
    CONSTRAINT uk_purest_category_code UNIQUE (code)
);

COMMENT ON TABLE purest_dict_category IS '字典分类';
COMMENT ON COLUMN purest_dict_category.id IS '主键Id';
COMMENT ON COLUMN purest_dict_category.create_by IS '创建人';
COMMENT ON COLUMN purest_dict_category.create_time IS '创建时间';
COMMENT ON COLUMN purest_dict_category.update_by IS '修改人';
COMMENT ON COLUMN purest_dict_category.update_time IS '修改时间';
COMMENT ON COLUMN purest_dict_category.remark IS '备注';
COMMENT ON COLUMN purest_dict_category.name IS '分类名称';
COMMENT ON COLUMN purest_dict_category.code IS '分类编码';

CREATE TABLE purest_dict_data (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    category_id bigint NOT NULL,
    name varchar(20) NOT NULL,
    code varchar(20) NOT NULL,
    sort numeric(10, 0) NOT NULL,
    CONSTRAINT pk_purest_dict_data PRIMARY KEY (id),
    CONSTRAINT fk_purest_dict_data_category FOREIGN KEY (category_id) REFERENCES purest_dict_category (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_dict_data IS '字典数据';
COMMENT ON COLUMN purest_dict_data.id IS '主键Id';
COMMENT ON COLUMN purest_dict_data.create_by IS '创建人';
COMMENT ON COLUMN purest_dict_data.create_time IS '创建时间';
COMMENT ON COLUMN purest_dict_data.update_by IS '修改人';
COMMENT ON COLUMN purest_dict_data.update_time IS '修改时间';
COMMENT ON COLUMN purest_dict_data.remark IS '备注';
COMMENT ON COLUMN purest_dict_data.category_id IS '字典分类ID';
COMMENT ON COLUMN purest_dict_data.name IS '字典名称';
COMMENT ON COLUMN purest_dict_data.code IS '编码';
COMMENT ON COLUMN purest_dict_data.sort IS '排序';

CREATE INDEX idx_purest_dict_data_category_id ON purest_dict_data (category_id);

CREATE TABLE purest_file_record (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    file_name varchar(100) NOT NULL,
    file_size numeric(10, 0) NOT NULL,
    file_ext varchar(10) NOT NULL,
    CONSTRAINT pk_purest_file_record PRIMARY KEY (id)
);

COMMENT ON TABLE purest_file_record IS '文件上传记录表';
COMMENT ON COLUMN purest_file_record.id IS 'Id';
COMMENT ON COLUMN purest_file_record.create_by IS '创建人';
COMMENT ON COLUMN purest_file_record.create_time IS '创建时间';
COMMENT ON COLUMN purest_file_record.update_by IS '修改人';
COMMENT ON COLUMN purest_file_record.update_time IS '修改时间';
COMMENT ON COLUMN purest_file_record.remark IS '备注';
COMMENT ON COLUMN purest_file_record.file_name IS '文件名';
COMMENT ON COLUMN purest_file_record.file_size IS '文件大小';
COMMENT ON COLUMN purest_file_record.file_ext IS '文件扩展名';

CREATE TABLE purest_function (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20) NOT NULL,
    code varchar(40) NOT NULL,
    parent_id bigint,
    CONSTRAINT pk_purest_function PRIMARY KEY (id),
    CONSTRAINT uk_purest_function_code UNIQUE (code)
);

COMMENT ON TABLE purest_function IS '功能表';
COMMENT ON COLUMN purest_function.id IS '主键Id';
COMMENT ON COLUMN purest_function.create_by IS '创建人';
COMMENT ON COLUMN purest_function.create_time IS '创建时间';
COMMENT ON COLUMN purest_function.update_by IS '修改人';
COMMENT ON COLUMN purest_function.update_time IS '修改时间';
COMMENT ON COLUMN purest_function.remark IS '备注';
COMMENT ON COLUMN purest_function.name IS '名称';
COMMENT ON COLUMN purest_function.code IS '编码';
COMMENT ON COLUMN purest_function.parent_id IS '隶属于';

CREATE TABLE purest_interface_group (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20),
    code varchar(40) NOT NULL,
    CONSTRAINT pk_purest_interface_group PRIMARY KEY (id),
    CONSTRAINT uk_purest_interface_group_code UNIQUE (code)
);

COMMENT ON TABLE purest_interface_group IS '接口分组表';
COMMENT ON COLUMN purest_interface_group.id IS '主键Id';
COMMENT ON COLUMN purest_interface_group.create_by IS '创建人';
COMMENT ON COLUMN purest_interface_group.create_time IS '创建时间';
COMMENT ON COLUMN purest_interface_group.update_by IS '修改人';
COMMENT ON COLUMN purest_interface_group.update_time IS '修改时间';
COMMENT ON COLUMN purest_interface_group.remark IS '备注';
COMMENT ON COLUMN purest_interface_group.name IS '名称';
COMMENT ON COLUMN purest_interface_group.code IS '编码';

CREATE TABLE purest_interface (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20) NOT NULL,
    path varchar(200) NOT NULL,
    request_method varchar(20) NOT NULL,
    group_id bigint,
    CONSTRAINT pk_purest_interface PRIMARY KEY (id),
    CONSTRAINT uk_interface_path_method UNIQUE (path, request_method),
    CONSTRAINT fk_purest_interface_group FOREIGN KEY (group_id) REFERENCES purest_interface_group (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_interface IS '接口表';
COMMENT ON COLUMN purest_interface.id IS '主键Id';
COMMENT ON COLUMN purest_interface.create_by IS '创建人';
COMMENT ON COLUMN purest_interface.create_time IS '创建时间';
COMMENT ON COLUMN purest_interface.update_by IS '修改人';
COMMENT ON COLUMN purest_interface.update_time IS '修改时间';
COMMENT ON COLUMN purest_interface.remark IS '备注';
COMMENT ON COLUMN purest_interface.name IS '接口名称';
COMMENT ON COLUMN purest_interface.path IS '接口地址';
COMMENT ON COLUMN purest_interface.request_method IS '请求方法';
COMMENT ON COLUMN purest_interface.group_id IS '接口分组ID';

CREATE INDEX idx_purest_interface_group_id ON purest_interface (group_id);

CREATE TABLE purest_function_interface (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    interface_id bigint,
    function_id bigint,
    CONSTRAINT pk_purest_function_interface PRIMARY KEY (id),
    CONSTRAINT fk_purest_function_interface_function FOREIGN KEY (function_id) REFERENCES purest_function (id) ON DELETE CASCADE,
    CONSTRAINT fk_purest_function_interface_interface FOREIGN KEY (interface_id) REFERENCES purest_interface (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_function_interface IS '页面接口表';
COMMENT ON COLUMN purest_function_interface.id IS '主键Id';
COMMENT ON COLUMN purest_function_interface.create_by IS '创建人';
COMMENT ON COLUMN purest_function_interface.create_time IS '创建时间';
COMMENT ON COLUMN purest_function_interface.update_by IS '修改人';
COMMENT ON COLUMN purest_function_interface.update_time IS '修改时间';
COMMENT ON COLUMN purest_function_interface.remark IS '备注';
COMMENT ON COLUMN purest_function_interface.interface_id IS '接口ID';
COMMENT ON COLUMN purest_function_interface.function_id IS '功能ID';

CREATE INDEX idx_purest_function_interface_function_id ON purest_function_interface (function_id);
CREATE INDEX idx_purest_function_interface_interface_id ON purest_function_interface (interface_id);

CREATE TABLE purest_notice (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    title varchar(40) NOT NULL,
    content text,
    notice_type bigint NOT NULL,
    level bigint,
    CONSTRAINT pk_purest_notice PRIMARY KEY (id)
);

COMMENT ON TABLE purest_notice IS '通知公告表';
COMMENT ON COLUMN purest_notice.id IS '主键Id';
COMMENT ON COLUMN purest_notice.create_by IS '创建人';
COMMENT ON COLUMN purest_notice.create_time IS '创建时间';
COMMENT ON COLUMN purest_notice.update_by IS '修改人';
COMMENT ON COLUMN purest_notice.update_time IS '修改时间';
COMMENT ON COLUMN purest_notice.remark IS '备注';
COMMENT ON COLUMN purest_notice.title IS '主题';
COMMENT ON COLUMN purest_notice.content IS '内容';
COMMENT ON COLUMN purest_notice.notice_type IS '类型';
COMMENT ON COLUMN purest_notice.level IS '级别';

CREATE TABLE purest_notice_record (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    receiver bigint NOT NULL,
    is_read boolean NOT NULL,
    notice_id bigint NOT NULL,
    CONSTRAINT pk_purest_notice_record PRIMARY KEY (id),
    CONSTRAINT fk_purest_notice_record_notice FOREIGN KEY (notice_id) REFERENCES purest_notice (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_notice_record IS '通知公告记录表';
COMMENT ON COLUMN purest_notice_record.id IS '主键Id';
COMMENT ON COLUMN purest_notice_record.create_by IS '创建人';
COMMENT ON COLUMN purest_notice_record.create_time IS '创建时间';
COMMENT ON COLUMN purest_notice_record.update_by IS '修改人';
COMMENT ON COLUMN purest_notice_record.update_time IS '修改时间';
COMMENT ON COLUMN purest_notice_record.remark IS '备注';
COMMENT ON COLUMN purest_notice_record.receiver IS '接收人';
COMMENT ON COLUMN purest_notice_record.is_read IS '是否已读';
COMMENT ON COLUMN purest_notice_record.notice_id IS '通知公告Id';

CREATE INDEX idx_purest_notice_record_notice_id ON purest_notice_record (notice_id);

CREATE TABLE purest_organization (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(100) NOT NULL,
    parent_id bigint,
    telephone varchar(20),
    leader varchar(20),
    sort numeric(10, 0),
    CONSTRAINT pk_purest_organization PRIMARY KEY (id),
    CONSTRAINT uk_purest_org_name_pid UNIQUE (name, parent_id),
    CONSTRAINT fk_purest_organization_parent FOREIGN KEY (parent_id) REFERENCES purest_organization (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_organization IS '组织机构';
COMMENT ON COLUMN purest_organization.id IS '主键Id';
COMMENT ON COLUMN purest_organization.create_by IS '创建人';
COMMENT ON COLUMN purest_organization.create_time IS '创建时间';
COMMENT ON COLUMN purest_organization.update_by IS '修改人';
COMMENT ON COLUMN purest_organization.update_time IS '修改时间';
COMMENT ON COLUMN purest_organization.remark IS '备注';
COMMENT ON COLUMN purest_organization.name IS '名称';
COMMENT ON COLUMN purest_organization.parent_id IS '父级ID';
COMMENT ON COLUMN purest_organization.telephone IS '联系电话';
COMMENT ON COLUMN purest_organization.leader IS '负责人';
COMMENT ON COLUMN purest_organization.sort IS '排序';

CREATE INDEX idx_purest_organization_parent_id ON purest_organization (parent_id);

CREATE TABLE purest_profile_system (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20) NOT NULL,
    code varchar(40) NOT NULL,
    file_id bigint NOT NULL,
    CONSTRAINT pk_purest_profile_system PRIMARY KEY (id),
    CONSTRAINT uk_purest_filesystem_code UNIQUE (code),
    CONSTRAINT fk_purest_profile_system_file FOREIGN KEY (file_id) REFERENCES purest_file_record (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_profile_system IS '系统文件表';
COMMENT ON COLUMN purest_profile_system.id IS '主键Id';
COMMENT ON COLUMN purest_profile_system.create_by IS '创建人';
COMMENT ON COLUMN purest_profile_system.create_time IS '创建时间';
COMMENT ON COLUMN purest_profile_system.update_by IS '修改人';
COMMENT ON COLUMN purest_profile_system.update_time IS '修改时间';
COMMENT ON COLUMN purest_profile_system.remark IS '备注';
COMMENT ON COLUMN purest_profile_system.name IS '名称';
COMMENT ON COLUMN purest_profile_system.code IS '编码';
COMMENT ON COLUMN purest_profile_system.file_id IS '文件ID';

CREATE INDEX idx_purest_profile_system_file_id ON purest_profile_system (file_id);

CREATE TABLE purest_request_log (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    controller_name varchar(100),
    action_name varchar(100),
    request_method varchar(10),
    environment_name varchar(20),
    elapsed_time numeric(16, 0),
    client_ip varchar(20),
    CONSTRAINT pk_purest_request_log PRIMARY KEY (id)
);

COMMENT ON TABLE purest_request_log IS '请求日志表';
COMMENT ON COLUMN purest_request_log.id IS '主键Id';
COMMENT ON COLUMN purest_request_log.create_by IS '创建人';
COMMENT ON COLUMN purest_request_log.create_time IS '创建时间';
COMMENT ON COLUMN purest_request_log.update_by IS '修改人';
COMMENT ON COLUMN purest_request_log.update_time IS '修改时间';
COMMENT ON COLUMN purest_request_log.remark IS '备注';
COMMENT ON COLUMN purest_request_log.controller_name IS '控制器';
COMMENT ON COLUMN purest_request_log.action_name IS '方法名';
COMMENT ON COLUMN purest_request_log.request_method IS '请求类型';
COMMENT ON COLUMN purest_request_log.environment_name IS '服务器环境';
COMMENT ON COLUMN purest_request_log.elapsed_time IS '执行耗时';
COMMENT ON COLUMN purest_request_log.client_ip IS '客户端IP';

CREATE TABLE purest_role (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20) NOT NULL,
    description varchar(200),
    CONSTRAINT pk_purest_role PRIMARY KEY (id),
    CONSTRAINT uk_purest_role_name UNIQUE (name)
);

COMMENT ON TABLE purest_role IS '角色';
COMMENT ON COLUMN purest_role.id IS '主键Id';
COMMENT ON COLUMN purest_role.create_by IS '创建人';
COMMENT ON COLUMN purest_role.create_time IS '创建时间';
COMMENT ON COLUMN purest_role.update_by IS '修改人';
COMMENT ON COLUMN purest_role.update_time IS '修改时间';
COMMENT ON COLUMN purest_role.remark IS '备注';
COMMENT ON COLUMN purest_role.name IS '角色名称';
COMMENT ON COLUMN purest_role.description IS '角色描述';

CREATE TABLE purest_role_function (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    role_id bigint,
    function_id bigint,
    CONSTRAINT pk_purest_role_function PRIMARY KEY (id),
    CONSTRAINT fk_purest_role_function_role FOREIGN KEY (role_id) REFERENCES purest_role (id) ON DELETE CASCADE,
    CONSTRAINT fk_purest_role_function_function FOREIGN KEY (function_id) REFERENCES purest_function (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_role_function IS '角色功能表';
COMMENT ON COLUMN purest_role_function.id IS '主键Id';
COMMENT ON COLUMN purest_role_function.create_by IS '创建人';
COMMENT ON COLUMN purest_role_function.create_time IS '创建时间';
COMMENT ON COLUMN purest_role_function.update_by IS '修改人';
COMMENT ON COLUMN purest_role_function.update_time IS '修改时间';
COMMENT ON COLUMN purest_role_function.remark IS '备注';
COMMENT ON COLUMN purest_role_function.role_id IS '角色ID';
COMMENT ON COLUMN purest_role_function.function_id IS '功能ID';

CREATE INDEX idx_purest_role_function_role_id ON purest_role_function (role_id);
CREATE INDEX idx_purest_role_function_function_id ON purest_role_function (function_id);

CREATE TABLE purest_system_config (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20),
    config_code varchar(40) NOT NULL,
    config_value varchar(1000),
    CONSTRAINT pk_purest_system_config PRIMARY KEY (id),
    CONSTRAINT uk_purest_config_code UNIQUE (config_code)
);

COMMENT ON TABLE purest_system_config IS '系统配置表';
COMMENT ON COLUMN purest_system_config.id IS '主键Id';
COMMENT ON COLUMN purest_system_config.create_by IS '创建人';
COMMENT ON COLUMN purest_system_config.create_time IS '创建时间';
COMMENT ON COLUMN purest_system_config.update_by IS '修改人';
COMMENT ON COLUMN purest_system_config.update_time IS '修改时间';
COMMENT ON COLUMN purest_system_config.remark IS '备注';
COMMENT ON COLUMN purest_system_config.name IS '名称';
COMMENT ON COLUMN purest_system_config.config_code IS '编码';
COMMENT ON COLUMN purest_system_config.config_value IS '值';

CREATE TABLE purest_user (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    account varchar(36) NOT NULL,
    password varchar(100) NOT NULL,
    name varchar(20) NOT NULL,
    telephone varchar(11),
    email varchar(20),
    avatar bytea,
    status integer,
    organization_id bigint NOT NULL,
    CONSTRAINT pk_purest_user PRIMARY KEY (id),
    CONSTRAINT uk_purest_user_account UNIQUE (account),
    CONSTRAINT fk_purest_user_organization FOREIGN KEY (organization_id) REFERENCES purest_organization (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_user IS '用户';
COMMENT ON COLUMN purest_user.id IS '主键Id';
COMMENT ON COLUMN purest_user.create_by IS '创建人';
COMMENT ON COLUMN purest_user.create_time IS '创建时间';
COMMENT ON COLUMN purest_user.update_by IS '修改人';
COMMENT ON COLUMN purest_user.update_time IS '修改时间';
COMMENT ON COLUMN purest_user.remark IS '备注';
COMMENT ON COLUMN purest_user.account IS '账号';
COMMENT ON COLUMN purest_user.password IS '密码';
COMMENT ON COLUMN purest_user.name IS '真实姓名';
COMMENT ON COLUMN purest_user.telephone IS '电话';
COMMENT ON COLUMN purest_user.email IS '邮箱';
COMMENT ON COLUMN purest_user.avatar IS '头像';
COMMENT ON COLUMN purest_user.status IS '状态';
COMMENT ON COLUMN purest_user.organization_id IS '组织机构Id';

CREATE INDEX idx_purest_user_organization_id ON purest_user (organization_id);

CREATE TABLE purest_user_role (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    role_id bigint NOT NULL,
    user_id bigint NOT NULL,
    CONSTRAINT pk_purest_user_role PRIMARY KEY (id),
    CONSTRAINT fk_purest_user_role_role FOREIGN KEY (role_id) REFERENCES purest_role (id) ON DELETE CASCADE,
    CONSTRAINT fk_purest_user_role_user FOREIGN KEY (user_id) REFERENCES purest_user (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_user_role IS '用户角色';
COMMENT ON COLUMN purest_user_role.id IS '主键Id';
COMMENT ON COLUMN purest_user_role.create_by IS '创建人';
COMMENT ON COLUMN purest_user_role.create_time IS '创建时间';
COMMENT ON COLUMN purest_user_role.update_by IS '修改人';
COMMENT ON COLUMN purest_user_role.update_time IS '修改时间';
COMMENT ON COLUMN purest_user_role.remark IS '备注';
COMMENT ON COLUMN purest_user_role.role_id IS '角色ID';
COMMENT ON COLUMN purest_user_role.user_id IS '用户ID';

CREATE INDEX idx_purest_user_role_role_id ON purest_user_role (role_id);
CREATE INDEX idx_purest_user_role_user_id ON purest_user_role (user_id);

CREATE TABLE purest_oauth2_user (
    persistence_id bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    id bigint,
    name varchar(20),
    type varchar(20) NOT NULL,
    user_id bigint,
    CONSTRAINT pk_purest_oauth2_user PRIMARY KEY (persistence_id),
    CONSTRAINT fk_purest_oauth2_user_user FOREIGN KEY (user_id) REFERENCES purest_user (id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_oauth2_user IS 'OAUTH2用户';
COMMENT ON COLUMN purest_oauth2_user.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_oauth2_user.create_time IS '创建时间';
COMMENT ON COLUMN purest_oauth2_user.id IS 'ID';
COMMENT ON COLUMN purest_oauth2_user.name IS '认证中心名';
COMMENT ON COLUMN purest_oauth2_user.type IS 'TYPE';
COMMENT ON COLUMN purest_oauth2_user.user_id IS '用户ID';

CREATE INDEX idx_purest_oauth2_user_user_id ON purest_oauth2_user (user_id);

CREATE TABLE purest_wf_definition (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    name varchar(20) NOT NULL,
    definition_id varchar(36) NOT NULL,
    workflow_content text NOT NULL,
    designs_content text NOT NULL,
    form_content text NOT NULL,
    version integer NOT NULL,
    is_locked boolean NOT NULL,
    CONSTRAINT pk_purest_wf_definition PRIMARY KEY (id),
    CONSTRAINT uk_workflow_code UNIQUE (definition_id)
);

COMMENT ON TABLE purest_wf_definition IS '流程定义';
COMMENT ON COLUMN purest_wf_definition.id IS '主键Id';
COMMENT ON COLUMN purest_wf_definition.create_by IS '创建人';
COMMENT ON COLUMN purest_wf_definition.create_time IS '创建时间';
COMMENT ON COLUMN purest_wf_definition.update_by IS '修改人';
COMMENT ON COLUMN purest_wf_definition.update_time IS '修改时间';
COMMENT ON COLUMN purest_wf_definition.remark IS '备注';
COMMENT ON COLUMN purest_wf_definition.name IS '名称';
COMMENT ON COLUMN purest_wf_definition.definition_id IS '定义ID';
COMMENT ON COLUMN purest_wf_definition.workflow_content IS '流程内容';
COMMENT ON COLUMN purest_wf_definition.designs_content IS '设计器内容';
COMMENT ON COLUMN purest_wf_definition.form_content IS '表单内容';
COMMENT ON COLUMN purest_wf_definition.version IS '版本';
COMMENT ON COLUMN purest_wf_definition.is_locked IS '是否锁定';

CREATE TABLE purest_wf_workflow (
    persistence_id bigint NOT NULL,
    complete_time timestamp(0),
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    data text,
    description varchar(500),
    instance_id varchar(36) NOT NULL,
    next_execution bigint,
    status integer NOT NULL,
    version integer NOT NULL,
    workflow_definition_id varchar(36) NOT NULL,
    reference varchar(200),
    remark varchar(1000),
    CONSTRAINT pk_purest_wf_workflow PRIMARY KEY (persistence_id)
);

COMMENT ON TABLE purest_wf_workflow IS '工作流程';
COMMENT ON COLUMN purest_wf_workflow.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_workflow.complete_time IS '完成时间';
COMMENT ON COLUMN purest_wf_workflow.create_by IS '创建人';
COMMENT ON COLUMN purest_wf_workflow.create_time IS '创建时间';
COMMENT ON COLUMN purest_wf_workflow.data IS '数据';
COMMENT ON COLUMN purest_wf_workflow.description IS '描述';
COMMENT ON COLUMN purest_wf_workflow.instance_id IS '实例ID';
COMMENT ON COLUMN purest_wf_workflow.next_execution IS '下一次执行';
COMMENT ON COLUMN purest_wf_workflow.status IS '状态';
COMMENT ON COLUMN purest_wf_workflow.version IS '版本';
COMMENT ON COLUMN purest_wf_workflow.workflow_definition_id IS '流程定义ID';
COMMENT ON COLUMN purest_wf_workflow.reference IS '引用';
COMMENT ON COLUMN purest_wf_workflow.remark IS '备注';

CREATE INDEX idx_purest_wf_workflow_instance_id ON purest_wf_workflow (instance_id);
CREATE INDEX idx_purest_wf_workflow_next_execution ON purest_wf_workflow (next_execution);

CREATE TABLE purest_wf_execution_pointer (
    persistence_id bigint NOT NULL,
    workflow_id bigint NOT NULL,
    id varchar(36),
    start_time timestamp(0),
    end_time timestamp(0),
    active boolean NOT NULL,
    event_key varchar(100),
    event_name varchar(100),
    event_data text,
    event_published boolean NOT NULL,
    persistence_data text,
    sleep_until timestamp(0),
    step_id integer NOT NULL,
    step_name varchar(100),
    children text,
    context_item text,
    predecessor_id varchar(100),
    outcome text,
    scope text,
    retry_count integer NOT NULL,
    status integer NOT NULL,
    CONSTRAINT pk_purest_wf_execution_pointer PRIMARY KEY (persistence_id),
    CONSTRAINT fk_purest_wf_execution_pointer_workflow FOREIGN KEY (workflow_id) REFERENCES purest_wf_workflow (persistence_id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_wf_execution_pointer IS '步骤';
COMMENT ON COLUMN purest_wf_execution_pointer.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_execution_pointer.workflow_id IS '工作流Id';
COMMENT ON COLUMN purest_wf_execution_pointer.id IS 'ID';
COMMENT ON COLUMN purest_wf_execution_pointer.start_time IS '开始时间';
COMMENT ON COLUMN purest_wf_execution_pointer.end_time IS '结束时间';
COMMENT ON COLUMN purest_wf_execution_pointer.active IS '是否激活';
COMMENT ON COLUMN purest_wf_execution_pointer.event_key IS '事件Key';
COMMENT ON COLUMN purest_wf_execution_pointer.event_name IS '事件名称';
COMMENT ON COLUMN purest_wf_execution_pointer.event_data IS '事件数据';
COMMENT ON COLUMN purest_wf_execution_pointer.event_published IS '事件是否发布';
COMMENT ON COLUMN purest_wf_execution_pointer.persistence_data IS '持久化数据';
COMMENT ON COLUMN purest_wf_execution_pointer.sleep_until IS '休眠至';
COMMENT ON COLUMN purest_wf_execution_pointer.step_id IS '步骤ID';
COMMENT ON COLUMN purest_wf_execution_pointer.step_name IS '步骤名称';
COMMENT ON COLUMN purest_wf_execution_pointer.children IS '子集';
COMMENT ON COLUMN purest_wf_execution_pointer.context_item IS '上下文';
COMMENT ON COLUMN purest_wf_execution_pointer.predecessor_id IS '前驱ID';
COMMENT ON COLUMN purest_wf_execution_pointer.outcome IS '输出';
COMMENT ON COLUMN purest_wf_execution_pointer.scope IS '范围';
COMMENT ON COLUMN purest_wf_execution_pointer.retry_count IS '重试次数';
COMMENT ON COLUMN purest_wf_execution_pointer.status IS '状态';

CREATE INDEX idx_purest_wf_execution_pointer_workflow_id ON purest_wf_execution_pointer (workflow_id);

CREATE TABLE purest_wf_execution_attribute (
    persistence_id bigint NOT NULL,
    attribute_key varchar(100),
    attribute_value text,
    execution_pointer_id bigint NOT NULL,
    CONSTRAINT pk_purest_wf_execution_attribute PRIMARY KEY (persistence_id),
    CONSTRAINT fk_purest_wf_execution_attribute_pointer FOREIGN KEY (execution_pointer_id) REFERENCES purest_wf_execution_pointer (persistence_id) ON DELETE CASCADE
);

COMMENT ON TABLE purest_wf_execution_attribute IS '自定义属性';
COMMENT ON COLUMN purest_wf_execution_attribute.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_execution_attribute.attribute_key IS '属性键';
COMMENT ON COLUMN purest_wf_execution_attribute.attribute_value IS '属性值';
COMMENT ON COLUMN purest_wf_execution_attribute.execution_pointer_id IS '执行指针Id';

CREATE INDEX idx_purest_wf_execution_attribute_pointer_id ON purest_wf_execution_attribute (execution_pointer_id);

CREATE TABLE purest_wf_execution_error (
    persistence_id bigint NOT NULL,
    error_time date NOT NULL,
    execution_pointer_id varchar(100),
    message text,
    workflow_id varchar(100),
    CONSTRAINT pk_purest_wf_execution_error PRIMARY KEY (persistence_id)
);

COMMENT ON TABLE purest_wf_execution_error IS '执行异常';
COMMENT ON COLUMN purest_wf_execution_error.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_execution_error.error_time IS '异常时间';
COMMENT ON COLUMN purest_wf_execution_error.execution_pointer_id IS '执行指针Id';
COMMENT ON COLUMN purest_wf_execution_error.message IS '消息';
COMMENT ON COLUMN purest_wf_execution_error.workflow_id IS '工作流Id';

CREATE TABLE purest_wf_event (
    persistence_id bigint NOT NULL,
    event_id varchar(36) NOT NULL,
    event_name varchar(200),
    event_key varchar(200),
    event_data text,
    event_time timestamp(0) NOT NULL,
    is_processed boolean NOT NULL,
    CONSTRAINT pk_purest_wf_event PRIMARY KEY (persistence_id),
    CONSTRAINT uk_purest_wf_event_event_id UNIQUE (event_id)
);

COMMENT ON TABLE purest_wf_event IS '事件';
COMMENT ON COLUMN purest_wf_event.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_event.event_id IS '事件ID';
COMMENT ON COLUMN purest_wf_event.event_name IS '事件名称';
COMMENT ON COLUMN purest_wf_event.event_key IS '事件Key';
COMMENT ON COLUMN purest_wf_event.event_data IS '事件数据';
COMMENT ON COLUMN purest_wf_event.event_time IS '事件时间';
COMMENT ON COLUMN purest_wf_event.is_processed IS '是否处理';

CREATE INDEX idx_purest_wf_event_event_time ON purest_wf_event (event_time);
CREATE INDEX idx_purest_wf_event_is_processed ON purest_wf_event (is_processed);
CREATE INDEX idx_purest_wf_event_name_key ON purest_wf_event (event_name, event_key);

CREATE TABLE purest_wf_scheduled_command (
    persistence_id bigint NOT NULL,
    command_name varchar(200),
    data varchar(500),
    execute_time bigint NOT NULL,
    CONSTRAINT pk_purest_wf_scheduled_command PRIMARY KEY (persistence_id),
    CONSTRAINT uk_purest_wf_scheduled_cmd UNIQUE (command_name, data)
);

COMMENT ON TABLE purest_wf_scheduled_command IS '计划命令';
COMMENT ON COLUMN purest_wf_scheduled_command.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_scheduled_command.command_name IS '命令名称';
COMMENT ON COLUMN purest_wf_scheduled_command.data IS '数据';
COMMENT ON COLUMN purest_wf_scheduled_command.execute_time IS '执行时间';

CREATE INDEX idx_purest_wf_scheduled_command_execute_time ON purest_wf_scheduled_command (execute_time);

CREATE TABLE purest_wf_subscription (
    persistence_id bigint NOT NULL,
    event_key varchar(200),
    event_name varchar(200),
    step_id integer NOT NULL,
    subscription_id varchar(36) NOT NULL,
    workflow_id varchar(200),
    subscribe_as_of timestamp(0) NOT NULL,
    subscription_data text,
    execution_pointer_id varchar(200),
    external_token varchar(200),
    external_token_expiry timestamp(0),
    external_worker_id varchar(200),
    CONSTRAINT pk_purest_wf_subscription PRIMARY KEY (persistence_id),
    CONSTRAINT uk_purest_wf_subscription_id UNIQUE (subscription_id)
);

COMMENT ON TABLE purest_wf_subscription IS '订阅';
COMMENT ON COLUMN purest_wf_subscription.persistence_id IS '主键Id';
COMMENT ON COLUMN purest_wf_subscription.event_key IS '事件Key';
COMMENT ON COLUMN purest_wf_subscription.event_name IS '事件名称';
COMMENT ON COLUMN purest_wf_subscription.step_id IS '步骤ID';
COMMENT ON COLUMN purest_wf_subscription.subscription_id IS '订阅ID';
COMMENT ON COLUMN purest_wf_subscription.workflow_id IS '工作流Id';
COMMENT ON COLUMN purest_wf_subscription.subscribe_as_of IS '订阅时间';
COMMENT ON COLUMN purest_wf_subscription.subscription_data IS '订阅数据';
COMMENT ON COLUMN purest_wf_subscription.execution_pointer_id IS '执行指针Id';
COMMENT ON COLUMN purest_wf_subscription.external_token IS '外部令牌';
COMMENT ON COLUMN purest_wf_subscription.external_token_expiry IS '外部令牌过期';
COMMENT ON COLUMN purest_wf_subscription.external_worker_id IS '外部工作者Id';

CREATE INDEX idx_purest_wf_subscription_event_key ON purest_wf_subscription (event_key);
CREATE INDEX idx_purest_wf_subscription_event_name ON purest_wf_subscription (event_name);

CREATE TABLE purest_wf_auditing_record (
    id bigint NOT NULL,
    execution_pointer_id bigint NOT NULL,
    auditing_time timestamp(0) NOT NULL,
    auditor bigint NOT NULL,
    auditor_name varchar(40),
    auditing_opinion text,
    is_agree boolean NOT NULL,
    CONSTRAINT pk_purest_wf_auditing_record PRIMARY KEY (id)
);

COMMENT ON TABLE purest_wf_auditing_record IS '流程审批记录';
COMMENT ON COLUMN purest_wf_auditing_record.id IS '主键Id';
COMMENT ON COLUMN purest_wf_auditing_record.execution_pointer_id IS '步骤Id';
COMMENT ON COLUMN purest_wf_auditing_record.auditing_time IS '审批时间';
COMMENT ON COLUMN purest_wf_auditing_record.auditor IS '审批人';
COMMENT ON COLUMN purest_wf_auditing_record.auditor_name IS '审批人姓名';
COMMENT ON COLUMN purest_wf_auditing_record.auditing_opinion IS '审批意见';
COMMENT ON COLUMN purest_wf_auditing_record.is_agree IS '是否同意';

CREATE TABLE purest_wf_waiting_pointer (
    id bigint NOT NULL,
    user_id bigint NOT NULL,
    pointer_id varchar(36) NOT NULL,
    CONSTRAINT pk_purest_wf_waiting_pointer PRIMARY KEY (id)
);

COMMENT ON TABLE purest_wf_waiting_pointer IS '待审核步骤';
COMMENT ON COLUMN purest_wf_waiting_pointer.id IS '主键Id';
COMMENT ON COLUMN purest_wf_waiting_pointer.user_id IS '用户Id';
COMMENT ON COLUMN purest_wf_waiting_pointer.pointer_id IS '步骤Id';

CREATE TABLE purest_workflow_instance (
    id bigint NOT NULL,
    create_by bigint NOT NULL,
    create_time timestamp(0) NOT NULL,
    update_by bigint,
    update_time timestamp(0),
    remark varchar(1000),
    wf_id bigint NOT NULL,
    scheme_id bigint NOT NULL,
    form_data text NOT NULL,
    current_node bigint,
    current_node_type integer NOT NULL,
    status integer NOT NULL,
    CONSTRAINT pk_purest_workflow_instance PRIMARY KEY (id),
    CONSTRAINT fk_purest_workflow_instance_workflow FOREIGN KEY (wf_id) REFERENCES purest_wf_workflow (persistence_id) ON DELETE RESTRICT,
    CONSTRAINT fk_purest_workflow_instance_scheme FOREIGN KEY (scheme_id) REFERENCES purest_wf_definition (id) ON DELETE RESTRICT
);

COMMENT ON TABLE purest_workflow_instance IS '流程实例';
COMMENT ON COLUMN purest_workflow_instance.id IS '主键Id';
COMMENT ON COLUMN purest_workflow_instance.create_by IS '创建人';
COMMENT ON COLUMN purest_workflow_instance.create_time IS '创建时间';
COMMENT ON COLUMN purest_workflow_instance.update_by IS '修改人';
COMMENT ON COLUMN purest_workflow_instance.update_time IS '修改时间';
COMMENT ON COLUMN purest_workflow_instance.remark IS '备注';
COMMENT ON COLUMN purest_workflow_instance.wf_id IS '流程ID';
COMMENT ON COLUMN purest_workflow_instance.scheme_id IS '设计ID';
COMMENT ON COLUMN purest_workflow_instance.form_data IS '表单值';
COMMENT ON COLUMN purest_workflow_instance.current_node IS '当前节点';
COMMENT ON COLUMN purest_workflow_instance.current_node_type IS '当前节点类型';
COMMENT ON COLUMN purest_workflow_instance.status IS '状态';

CREATE INDEX idx_purest_workflow_instance_wf_id ON purest_workflow_instance (wf_id);
CREATE INDEX idx_purest_workflow_instance_scheme_id ON purest_workflow_instance (scheme_id);

COMMIT;








