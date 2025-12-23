-- 远程会诊演示数据种子（PostgreSQL 16）
-- 执行顺序：
-- 1) 03.1_admin-postgresql.sql
-- 2) 03.2_purest_rc_business.sql
-- 3) 03.3_purest_rc_menu.sql
-- 4) 本脚本
--
-- 说明：
-- - 用于“功能流程演示”，不依赖真实三方数据源
-- - 账号默认密码：123456（后端登录会进行 MD5 校验）
-- - 本脚本尽量幂等：大部分使用固定ID + ON CONFLICT

BEGIN;

-- 0. 演示机构
INSERT INTO purest_organization (
  id, create_by, create_time, update_by, update_time, remark,
  name, parent_id, telephone, leader, sort
)
VALUES
  (9900000000011, 0, now(), NULL, NULL, '演示机构-本院', '演示医院（本院）', NULL, '0731-0000001', '演示管理员', 1),
  (9900000000012, 0, now(), NULL, NULL, '演示机构-上级', '演示医院（上级）', NULL, '0731-0000002', '演示管理员', 2)
ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  remark = EXCLUDED.remark,
  telephone = EXCLUDED.telephone,
  leader = EXCLUDED.leader,
  sort = EXCLUDED.sort,
  update_time = now();

-- 1. 演示角色（不同角色对应不同权限）
INSERT INTO purest_role (
  id, create_by, create_time, update_by, update_time, remark,
  name, description
)
VALUES
  (9900000200001, 0, now(), NULL, NULL, '演示主持人角色', '演示角色（主持人）', '拥有会诊全流程操作权限'),
  (9900000200002, 0, now(), NULL, NULL, '演示专家角色',   '演示角色（专家）',   '拥有查看与报告签署权限'),
  (9900000200003, 0, now(), NULL, NULL, '演示观察员角色', '演示角色（观察员）', '仅拥有查看权限')
ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  remark = EXCLUDED.remark,
  update_time = now();

-- 2. 演示账号（与 RH.Admin/src/config/demoUsers.ts 对齐）
-- MD5(123456) = e10adc3949ba59abbe56e057f20f883e
INSERT INTO purest_user (
  id, create_by, create_time, update_by, update_time, remark,
  account, password, name, telephone, email, status, organization_id
)
VALUES
  (9900000100001, 0, now(), NULL, NULL, '演示账号-主持人', 'demo_host', 'e10adc3949ba59abbe56e057f20f883e', '王医生(主持人)', '13800000001', NULL, 1, 9900000000011),
  (9900000100002, 0, now(), NULL, NULL, '演示账号-专家',   'demo_expert', 'e10adc3949ba59abbe56e057f20f883e', '李教授(专家)',   '13800000002', NULL, 1, 9900000000012),
  (9900000100003, 0, now(), NULL, NULL, '演示账号-观察员', 'demo_observer', 'e10adc3949ba59abbe56e057f20f883e', '实习生(观察员)', '13800000003', NULL, 1, 9900000000012)
ON CONFLICT (id) DO UPDATE SET
  account = EXCLUDED.account,
  password = EXCLUDED.password,
  name = EXCLUDED.name,
  status = EXCLUDED.status,
  organization_id = EXCLUDED.organization_id,
  update_time = now();

-- 3. 绑定演示账号到对应演示角色
INSERT INTO purest_user_role (id, create_by, create_time, update_by, update_time, remark, role_id, user_id)
VALUES
  (9900000400001, 0, now(), NULL, NULL, '演示主持人绑定', 9900000200001, 9900000100001),
  (9900000400002, 0, now(), NULL, NULL, '演示专家绑定',   9900000200002, 9900000100002),
  (9900000400003, 0, now(), NULL, NULL, '演示观察员绑定', 9900000200003, 9900000100003)
ON CONFLICT (id) DO UPDATE SET
  role_id = EXCLUDED.role_id,
  user_id = EXCLUDED.user_id,
  update_time = now();

-- 4. 权限细分：
-- Host: Full remote% permissions
INSERT INTO purest_role_function (id, create_by, create_time, role_id, function_id)
SELECT 9900000500000 + id, 0, now(), 9900000200001, id 
FROM purest_function WHERE code LIKE 'remote%'
ON CONFLICT (id) DO NOTHING;

-- Expert: View, Report, Member, Attachment permissions
INSERT INTO purest_role_function (id, create_by, create_time, role_id, function_id)
SELECT 9900000600000 + id, 0, now(), 9900000200002, id 
FROM purest_function WHERE code IN ('remote', 'remote.consultation', 'remote.consultation.view', 'remote.consultation.report', 'remote.consultation.member', 'remote.consultation.attachment')
ON CONFLICT (id) DO NOTHING;

-- Observer: View permissions only
INSERT INTO purest_role_function (id, create_by, create_time, role_id, function_id)
SELECT 9900000700000 + id, 0, now(), 9900000200003, id 
FROM purest_function WHERE code IN ('remote', 'remote.consultation', 'remote.consultation.view', 'remote.consultation.attachment')
ON CONFLICT (id) DO NOTHING;

-- 5. 演示文件（仅用于列表展示/附件引用）
INSERT INTO purest_file_record (
  id, create_by, create_time, update_by, update_time, remark,
  file_name, file_size, file_ext
)
VALUES
  (9900000600001, 0, now(), NULL, NULL, '演示影像', '超声关键切面-1.jpg', 234567, 'jpg'),
  (9900000600002, 0, now(), NULL, NULL, '演示影像', '超声关键切面-2.jpg', 198765, 'jpg'),
  (9900000600003, 0, now(), NULL, NULL, '演示检验', '血常规报告.pdf', 456789, 'pdf'),
  (9900000600004, 0, now(), NULL, NULL, '演示病历', '门诊病历摘要.pdf', 123456, 'pdf'),
  (9900000600005, 0, now(), NULL, NULL, '演示签章', '签署印章.svg', 2048, 'svg')
ON CONFLICT (id) DO UPDATE SET
  file_name = EXCLUDED.file_name,
  file_size = EXCLUDED.file_size,
  file_ext = EXCLUDED.file_ext,
  remark = EXCLUDED.remark,
  update_time = now();

-- 6. 患者快照（3套案例）
INSERT INTO purest_rc_patient_case (
  id, create_by, create_time, update_by, update_time, remark,
  his_patient_id, his_visit_id,
  patient_name, patient_sex, patient_birth, patient_type,
  id_card, phone, contact_address,
  medical_summary, allergies, gestational_weeks, child_age_months,
  data_source
)
VALUES
  (
    990001000001, 0, now(), NULL, NULL, '演示-孕产案例',
    'HIS-P-001', 'VISIT-001',
    '张某某', '女', '1995-04-12', 'PREGNANT',
    '4301************', '13800010001', '演示地址-本院',
    '孕中期超声提示异常回声，申请远程会诊。', NULL, 24.5, NULL,
    'MANUAL'
  ),
  (
    990001000002, 0, now(), NULL, NULL, '演示-儿科案例',
    'HIS-P-002', 'VISIT-002',
    '李某某', '男', '2021-08-05', 'CHILD',
    NULL, '13800010002', '演示地址-本院',
    '发热咳嗽3天，精神差，申请远程会诊。', NULL, NULL, 40,
    'MANUAL'
  ),
  (
    990001000003, 0, now(), NULL, NULL, '演示-普通案例',
    'HIS-P-003', 'VISIT-003',
    '王某某', '女', '1988-11-20', 'OTHER',
    NULL, '13800010003', '演示地址-上级',
    '一般咨询/复诊评估。', NULL, NULL, NULL,
    'MANUAL'
  )
ON CONFLICT (id) DO UPDATE SET
  patient_name = EXCLUDED.patient_name,
  patient_sex = EXCLUDED.patient_sex,
  patient_birth = EXCLUDED.patient_birth,
  patient_type = EXCLUDED.patient_type,
  phone = EXCLUDED.phone,
  contact_address = EXCLUDED.contact_address,
  medical_summary = EXCLUDED.medical_summary,
  gestational_weeks = EXCLUDED.gestational_weeks,
  child_age_months = EXCLUDED.child_age_months,
  data_source = EXCLUDED.data_source,
  remark = EXCLUDED.remark,
  update_time = now();

-- 7. 同步任务（用于演示“执行(模拟)”与状态流转）
INSERT INTO purest_rc_sync_task (
  id, create_by, create_time, update_by, update_time, remark,
  task_no, task_type, task_status, request_payload, response_payload, completed_time, retry_count
)
VALUES
  (
    990002400001, 0, now(), NULL, NULL, '演示：患者资料同步（待执行）',
    'DEMO-SYNC-001', 'PATIENT', 'PENDING',
    jsonb_build_object('patientId','HIS-P-001','caseId',990001000001),
    NULL, NULL, 0
  ),
  (
    990002400002, 0, now(), NULL, NULL, '演示：检验报告同步（可模拟执行）',
    'DEMO-SYNC-002', 'LAB', 'PENDING',
    jsonb_build_object('patientId','HIS-P-002','caseId',990001000002),
    NULL, NULL, 0
  ),
  (
    990002400003, 0, now(), NULL, NULL, '演示：影像资料同步（已成功）',
    'DEMO-SYNC-003', 'IMAGE', 'SUCCESS',
    jsonb_build_object('patientId','HIS-P-001','caseId',990001000001),
    jsonb_build_object('ok',true,'message','演示环境预置成功'),
    now(), 0
  )
ON CONFLICT (id) DO UPDATE SET
  task_no = EXCLUDED.task_no,
  task_type = EXCLUDED.task_type,
  task_status = EXCLUDED.task_status,
  request_payload = EXCLUDED.request_payload,
  response_payload = EXCLUDED.response_payload,
  completed_time = EXCLUDED.completed_time,
  retry_count = EXCLUDED.retry_count,
  remark = EXCLUDED.remark,
  update_time = now();

-- 8. 资料包（每个患者一个资料包）
INSERT INTO purest_rc_patient_pack (
  id, create_by, create_time, update_by, update_time, remark,
  case_id, pack_no, pack_status, expire_time, file_count, total_size, sync_task_id
)
VALUES
  (
    990001100001, 0, now(), NULL, NULL, '演示资料包-孕产',
    990001000001, 'PACK-DEMO-001', 'COMPLETED', now() + interval '7 day', 2, 433332, 990002400003
  ),
  (
    990001100002, 0, now(), NULL, NULL, '演示资料包-儿科',
    990001000002, 'PACK-DEMO-002', 'COMPLETED', now() + interval '7 day', 2, 580245, 990002400002
  ),
  (
    990001100003, 0, now(), NULL, NULL, '演示资料包-普通',
    990001000003, 'PACK-DEMO-003', 'COMPLETED', now() + interval '7 day', 1, 123456, NULL
  )
ON CONFLICT (id) DO UPDATE SET
  case_id = EXCLUDED.case_id,
  pack_no = EXCLUDED.pack_no,
  pack_status = EXCLUDED.pack_status,
  expire_time = EXCLUDED.expire_time,
  file_count = EXCLUDED.file_count,
  total_size = EXCLUDED.total_size,
  sync_task_id = EXCLUDED.sync_task_id,
  remark = EXCLUDED.remark,
  update_time = now();

-- 9. 资料包文件明细
INSERT INTO purest_rc_patient_pack_file (
  id, create_by, create_time, update_by, update_time, remark,
  pack_id, file_id, file_name, file_size
)
VALUES
  (990001110001, 0, now(), NULL, NULL, NULL, 990001100001, 9900000600001, '超声关键切面-1.jpg', 234567),
  (990001110002, 0, now(), NULL, NULL, NULL, 990001100001, 9900000600002, '超声关键切面-2.jpg', 198765),
  (990001110003, 0, now(), NULL, NULL, NULL, 990001100002, 9900000600003, '血常规报告.pdf', 456789),
  (990001110004, 0, now(), NULL, NULL, NULL, 990001100002, 9900000600004, '门诊病历摘要.pdf', 123456),
  (990001110005, 0, now(), NULL, NULL, NULL, 990001100003, 9900000600004, '门诊病历摘要.pdf', 123456)
ON CONFLICT (id) DO UPDATE SET
  pack_id = EXCLUDED.pack_id,
  file_id = EXCLUDED.file_id,
  file_name = EXCLUDED.file_name,
  file_size = EXCLUDED.file_size,
  update_time = now();

-- 10. 会诊（3条：已排期/会诊中/已完成+已签署）
INSERT INTO purest_rc_consultation (
  id, create_by, create_time, update_by, update_time, remark,
  case_id, pack_id,
  apply_org_id, apply_doctor_id, target_org_id, target_department, target_expert_id,
  purpose, emergency_level, consultation_status,
  desired_start_time, desired_end_time,
  scheduled_start_time, scheduled_end_time,
  meeting_room_no, rtc_channel_id, rtc_vendor,
  audit_doctor_id, audit_time, audit_comment, close_reason
)
VALUES
  (
    990002000001, 0, now(), NULL, NULL, '演示会诊-已排期',
    990001000001, 990001100001,
    9900000000011, (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1),
    9900000000012, '产科', (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1),
    '孕产疑难超声远程会诊（演示）', 'NORMAL', 'SCHEDULED',
    now() + interval '1 day', now() + interval '1 day' + interval '30 minute',
    now() + interval '1 day', now() + interval '1 day' + interval '30 minute',
    'rc-990002000001', 'demo-channel-001', 'PY_SFU',
    (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1), now(), '演示审核通过', NULL
  ),
  (
    990002000002, 0, now(), NULL, NULL, '演示会诊-会诊中',
    990001000002, 990001100002,
    9900000000011, (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1),
    9900000000012, '儿科', (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1),
    '儿科急症远程会诊（演示）', 'URGENT', 'IN_PROGRESS',
    now(), now() + interval '1 hour',
    now() - interval '10 minute', now() + interval '20 minute',
    'rc-990002000002', 'demo-channel-002', 'PY_SFU',
    (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1), now() - interval '30 minute', '演示审核通过', NULL
  ),
  (
    990002000003, 0, now(), NULL, NULL, '演示会诊-已完成已签署',
    990001000003, 990001100003,
    9900000000012, (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1),
    9900000000011, '妇科', (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1),
    '远程咨询/复诊评估（演示）', 'NORMAL', 'FINISHED',
    now() - interval '2 day', now() - interval '2 day' + interval '30 minute',
    now() - interval '2 day', now() - interval '2 day' + interval '30 minute',
    'rc-990002000003', 'demo-channel-003', 'PY_SFU',
    (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1), now() - interval '2 day' - interval '1 hour', '演示审核通过', NULL
  )
ON CONFLICT (id) DO UPDATE SET
  case_id = EXCLUDED.case_id,
  pack_id = EXCLUDED.pack_id,
  apply_org_id = EXCLUDED.apply_org_id,
  apply_doctor_id = EXCLUDED.apply_doctor_id,
  target_org_id = EXCLUDED.target_org_id,
  target_department = EXCLUDED.target_department,
  target_expert_id = EXCLUDED.target_expert_id,
  purpose = EXCLUDED.purpose,
  emergency_level = EXCLUDED.emergency_level,
  consultation_status = EXCLUDED.consultation_status,
  desired_start_time = EXCLUDED.desired_start_time,
  desired_end_time = EXCLUDED.desired_end_time,
  scheduled_start_time = EXCLUDED.scheduled_start_time,
  scheduled_end_time = EXCLUDED.scheduled_end_time,
  meeting_room_no = EXCLUDED.meeting_room_no,
  rtc_channel_id = EXCLUDED.rtc_channel_id,
  rtc_vendor = EXCLUDED.rtc_vendor,
  audit_doctor_id = EXCLUDED.audit_doctor_id,
  audit_time = EXCLUDED.audit_time,
  audit_comment = EXCLUDED.audit_comment,
  close_reason = EXCLUDED.close_reason,
  remark = EXCLUDED.remark,
  update_time = now();

-- 11. 会诊成员（每个会诊 3 角色）
INSERT INTO purest_rc_consultation_member (
  id, create_by, create_time, update_by, update_time, remark,
  consultation_id, user_id, org_id, role_code, join_status, join_time, leave_time
)
VALUES
  -- 会诊 1：已排期
  (990002100001, 0, now(), NULL, NULL, NULL, 990002000001, (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1), 9900000000011, 'HOST', 'CONFIRMED', now() - interval '1 hour', NULL),
  (990002100002, 0, now(), NULL, NULL, NULL, 990002000001, (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1), 9900000000012, 'EXPERT', 'CONFIRMED', now() - interval '50 minute', NULL),
  (990002100003, 0, now(), NULL, NULL, NULL, 990002000001, (SELECT id FROM purest_user WHERE account='demo_observer' LIMIT 1), 9900000000012, 'OBSERVER', 'CONFIRMED', now() - interval '40 minute', NULL),

  -- 会诊 2：会诊中
  (990002100004, 0, now(), NULL, NULL, NULL, 990002000002, (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1), 9900000000011, 'HOST', 'IN_PROGRESS', now() - interval '20 minute', NULL),
  (990002100005, 0, now(), NULL, NULL, NULL, 990002000002, (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1), 9900000000012, 'EXPERT', 'IN_PROGRESS', now() - interval '18 minute', NULL),
  (990002100006, 0, now(), NULL, NULL, NULL, 990002000002, (SELECT id FROM purest_user WHERE account='demo_observer' LIMIT 1), 9900000000012, 'OBSERVER', 'IN_PROGRESS', now() - interval '15 minute', NULL),

  -- 会诊 3：已完成
  (990002100007, 0, now(), NULL, NULL, NULL, 990002000003, (SELECT id FROM purest_user WHERE account='demo_host' LIMIT 1), 9900000000012, 'HOST', 'EXITED', now() - interval '2 day', now() - interval '2 day' + interval '30 minute'),
  (990002100008, 0, now(), NULL, NULL, NULL, 990002000003, (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1), 9900000000011, 'EXPERT', 'EXITED', now() - interval '2 day', now() - interval '2 day' + interval '30 minute'),
  (990002100009, 0, now(), NULL, NULL, NULL, 990002000003, (SELECT id FROM purest_user WHERE account='demo_observer' LIMIT 1), 9900000000011, 'OBSERVER', 'EXITED', now() - interval '2 day', now() - interval '2 day' + interval '30 minute')
ON CONFLICT (id) DO UPDATE SET
  consultation_id = EXCLUDED.consultation_id,
  user_id = EXCLUDED.user_id,
  org_id = EXCLUDED.org_id,
  role_code = EXCLUDED.role_code,
  join_status = EXCLUDED.join_status,
  join_time = EXCLUDED.join_time,
  leave_time = EXCLUDED.leave_time,
  update_time = now();

-- 12. 会诊附件（演示：把资料包文件作为会诊附件/补充材料）
INSERT INTO purest_rc_consultation_attachment (
  id, create_by, create_time, update_by, update_time, remark,
  consultation_id, file_id, external_uri, attachment_type, source_system, description
)
VALUES
  (990002500001, 0, now(), NULL, NULL, NULL, 990002000001, 9900000600001, NULL, 'PACS', 'MANUAL', '演示影像附件'),
  (990002500002, 0, now(), NULL, NULL, NULL, 990002000002, 9900000600003, NULL, 'LIS', 'MANUAL', '演示检验报告'),
  (990002500003, 0, now(), NULL, NULL, NULL, 990002000003, 9900000600004, NULL, 'EMR', 'MANUAL', '演示病历摘要')
ON CONFLICT (id) DO UPDATE SET
  consultation_id = EXCLUDED.consultation_id,
  file_id = EXCLUDED.file_id,
  attachment_type = EXCLUDED.attachment_type,
  source_system = EXCLUDED.source_system,
  description = EXCLUDED.description,
  update_time = now();

-- 13. 时间线（用于前端时间线展示）
INSERT INTO purest_rc_consultation_timeline (
  id, create_by, create_time, update_by, update_time, remark,
  consultation_id, event_code, event_content, snapshot
)
VALUES
  -- 会诊1：已排期
  (990002200001, 0, now() - interval '3 day', NULL, NULL, NULL, 990002000001, 'SUBMITTED', '提交申请', NULL),
  (990002200002, 0, now() - interval '3 day' + interval '5 minute', NULL, NULL, NULL, 990002000001, 'APPROVED', '审核通过', NULL),
  (990002200003, 0, now() - interval '2 day', NULL, NULL, NULL, 990002000001, 'SCHEDULED', '排期完成', NULL),

  -- 会诊2：会诊中
  (990002200004, 0, now() - interval '1 day', NULL, NULL, NULL, 990002000002, 'SUBMITTED', '提交申请', NULL),
  (990002200005, 0, now() - interval '1 day' + interval '10 minute', NULL, NULL, NULL, 990002000002, 'APPROVED', '审核通过', NULL),
  (990002200006, 0, now() - interval '1 hour', NULL, NULL, NULL, 990002000002, 'SCHEDULED', '排期完成', NULL),
  (990002200007, 0, now() - interval '10 minute', NULL, NULL, NULL, 990002000002, 'STARTED', '会诊开始', NULL),

  -- 会诊3：已完成已签署
  (990002200008, 0, now() - interval '3 day', NULL, NULL, NULL, 990002000003, 'SUBMITTED', '提交申请', NULL),
  (990002200009, 0, now() - interval '3 day' + interval '10 minute', NULL, NULL, NULL, 990002000003, 'APPROVED', '审核通过', NULL),
  (990002200010, 0, now() - interval '2 day', NULL, NULL, NULL, 990002000003, 'SCHEDULED', '排期完成', NULL),
  (990002200011, 0, now() - interval '2 day', NULL, NULL, NULL, 990002000003, 'STARTED', '会诊开始', NULL),
  (990002200012, 0, now() - interval '2 day' + interval '30 minute', NULL, NULL, NULL, 990002000003, 'ENDED', '会诊结束', NULL),
  (990002200013, 0, now() - interval '2 day' + interval '40 minute', NULL, NULL, NULL, 990002000003, 'SIGNED', '报告签署', NULL)
ON CONFLICT (id) DO UPDATE SET
  consultation_id = EXCLUDED.consultation_id,
  event_code = EXCLUDED.event_code,
  event_content = EXCLUDED.event_content,
  snapshot = EXCLUDED.snapshot,
  update_time = now();

-- 14. 会诊报告（会诊3 预置为已签署）
INSERT INTO purest_rc_consultation_report (
  id, create_by, create_time, update_by, update_time, remark,
  consultation_id, summary, diagnosis, treatment_advice, follow_up_plan,
  signer_id, signed_time, signature_file_id, report_status
)
VALUES
  (
    990002300001, 0, now() - interval '1 day', NULL, NULL, '演示报告-草稿',
    990002000001,
    '演示报告（草稿）：孕产疑难超声会诊摘要',
    '待完善',
    '待完善',
    '待完善',
    NULL, NULL, NULL, 'DRAFT'
  ),
  (
    990002300002, 0, now() - interval '1 hour', NULL, NULL, '演示报告-签署中',
    990002000002,
    '演示报告（签署中）：儿科急症评估摘要',
    '待完善',
    '待完善',
    '待完善',
    NULL, NULL, NULL, 'SIGNING'
  ),
  (
    990002300003, 0, now() - interval '2 day', NULL, NULL, '演示报告-已签署',
    990002000003,
    '演示报告：远程咨询/复诊评估摘要',
    '诊断建议：结合既往资料进一步评估。',
    '治疗建议：对症处理，必要时线下复诊。',
    '随访计划：1-2周内复诊或按医嘱随访。',
    (SELECT id FROM purest_user WHERE account='demo_expert' LIMIT 1),
    now() - interval '2 day' + interval '40 minute',
    9900000600005,
    'SIGNED'
  )
ON CONFLICT (id) DO UPDATE SET
  consultation_id = EXCLUDED.consultation_id,
  summary = EXCLUDED.summary,
  diagnosis = EXCLUDED.diagnosis,
  treatment_advice = EXCLUDED.treatment_advice,
  follow_up_plan = EXCLUDED.follow_up_plan,
  signer_id = EXCLUDED.signer_id,
  signed_time = EXCLUDED.signed_time,
  signature_file_id = EXCLUDED.signature_file_id,
  report_status = EXCLUDED.report_status,
  remark = EXCLUDED.remark,
  update_time = now();

COMMIT;


