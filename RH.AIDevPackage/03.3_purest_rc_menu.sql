-- 远程会诊菜单与接口授权初始化脚本
-- 执行顺序：在 admin-postgresql.sql 与 purest_rc_business.sql 之后执行

BEGIN;

-- 1. 菜单/功能节点
INSERT INTO purest_function (id, create_by, create_time, update_by, update_time, remark, name, code, parent_id)
VALUES
    (9700001, 0, now(), NULL, NULL, '远程会诊业务根节点', '远程会诊', 'remote', 0),

    (9700101, 0, now(), NULL, NULL, '远程会诊-会诊管理菜单', '会诊管理', 'remote.consultation', 9700001),
    (9700102, 0, now(), NULL, NULL, NULL, '新增会诊', 'remote.consultation.add', 9700101),
    (9700103, 0, now(), NULL, NULL, NULL, '编辑会诊', 'remote.consultation.edit', 9700101),
    (9700104, 0, now(), NULL, NULL, NULL, '查看会诊', 'remote.consultation.view', 9700101),
    (9700105, 0, now(), NULL, NULL, NULL, '删除会诊', 'remote.consultation.delete', 9700101),
    (9700106, 0, now(), NULL, NULL, NULL, '审核会诊', 'remote.consultation.audit', 9700101),
    (9700107, 0, now(), NULL, NULL, NULL, '排期会诊', 'remote.consultation.schedule', 9700101),
    (9700108, 0, now(), NULL, NULL, NULL, '启动会诊', 'remote.consultation.start', 9700101),
    (9700109, 0, now(), NULL, NULL, NULL, '结束会诊', 'remote.consultation.finish', 9700101),
    (9700110, 0, now(), NULL, NULL, NULL, '关闭会诊', 'remote.consultation.close', 9700101),
    (9700111, 0, now(), NULL, NULL, NULL, '音视频配置', 'remote.consultation.rtc', 9700101),
    (9700112, 0, now(), NULL, NULL, NULL, '报告维护', 'remote.consultation.report', 9700101),
    (9700113, 0, now(), NULL, NULL, NULL, '成员维护', 'remote.consultation.member', 9700101),
    (9700114, 0, now(), NULL, NULL, NULL, '附件维护', 'remote.consultation.attachment', 9700101),

    (9700201, 0, now(), NULL, NULL, '远程会诊-患者快照菜单', '患者快照', 'remote.patientCase', 9700001),
    (9700202, 0, now(), NULL, NULL, NULL, '新增快照', 'remote.patientCase.add', 9700201),
    (9700203, 0, now(), NULL, NULL, NULL, '编辑快照', 'remote.patientCase.edit', 9700201),
    (9700204, 0, now(), NULL, NULL, NULL, '查看快照', 'remote.patientCase.view', 9700201),
    (9700205, 0, now(), NULL, NULL, NULL, '删除快照', 'remote.patientCase.delete', 9700201),

    (9700301, 0, now(), NULL, NULL, '远程会诊-资料包菜单', '资料包管理', 'remote.patientPack', 9700001),
    (9700302, 0, now(), NULL, NULL, NULL, '新增资料包', 'remote.patientPack.add', 9700301),
    (9700303, 0, now(), NULL, NULL, NULL, '编辑资料包', 'remote.patientPack.edit', 9700301),
    (9700304, 0, now(), NULL, NULL, NULL, '查看资料包', 'remote.patientPack.view', 9700301),
    (9700305, 0, now(), NULL, NULL, NULL, '删除资料包', 'remote.patientPack.delete', 9700301),

    (9700401, 0, now(), NULL, NULL, '远程会诊-同步任务菜单', '同步任务', 'remote.syncTask', 9700001),
    (9700402, 0, now(), NULL, NULL, NULL, '新增任务', 'remote.syncTask.add', 9700401),
    (9700403, 0, now(), NULL, NULL, NULL, '编辑任务', 'remote.syncTask.edit', 9700401),
    (9700404, 0, now(), NULL, NULL, NULL, '查看任务', 'remote.syncTask.view', 9700401),
    (9700405, 0, now(), NULL, NULL, NULL, '删除任务', 'remote.syncTask.delete', 9700401)
ON CONFLICT (id)
DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, parent_id = EXCLUDED.parent_id, remark = EXCLUDED.remark;

-- 2. 接口分组
INSERT INTO purest_interface_group (id, create_by, create_time, update_by, update_time, remark, name, code)
VALUES
    (9750001, 0, now(), NULL, NULL, '远程会诊接口分组', '远程会诊', 'remoteHealthcare')
ON CONFLICT (id)
DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, remark = EXCLUDED.remark;

-- 3. 接口定义
INSERT INTO purest_interface (id, create_by, create_time, update_by, update_time, remark, name, path, request_method, group_id)
VALUES
    (9750101, 0, now(), NULL, NULL, NULL, '会诊分页', '/rc-consultation', 'GET', 9750001),
    (9750102, 0, now(), NULL, NULL, NULL, '会诊详情', '/rc-consultation/{id}', 'GET', 9750001),
    (9750103, 0, now(), NULL, NULL, NULL, '创建会诊', '/rc-consultation', 'POST', 9750001),
    (9750104, 0, now(), NULL, NULL, NULL, '更新会诊', '/rc-consultation/{id}', 'PUT', 9750001),
    (9750105, 0, now(), NULL, NULL, NULL, '删除会诊', '/rc-consultation/{id}', 'DELETE', 9750001),
    (9750106, 0, now(), NULL, NULL, NULL, '审核会诊', '/rc-consultation/audit', 'POST', 9750001),
    (9750107, 0, now(), NULL, NULL, NULL, '排期会诊', '/rc-consultation/schedule', 'POST', 9750001),
    (9750108, 0, now(), NULL, NULL, NULL, '音视频更新', '/rc-consultation/rtc', 'POST', 9750001),
    (9750109, 0, now(), NULL, NULL, NULL, '开始会诊', '/rc-consultation/start', 'POST', 9750001),
    (9750110, 0, now(), NULL, NULL, NULL, '结束会诊', '/rc-consultation/finish', 'POST', 9750001),
    (9750111, 0, now(), NULL, NULL, NULL, '关闭会诊', '/rc-consultation/close', 'POST', 9750001),

    (9750201, 0, now(), NULL, NULL, NULL, '附件分页', '/rc-consultation-attachment', 'GET', 9750001),
    (9750202, 0, now(), NULL, NULL, NULL, '新增附件', '/rc-consultation-attachment', 'POST', 9750001),
    (9750203, 0, now(), NULL, NULL, NULL, '更新附件', '/rc-consultation-attachment/{id}', 'PUT', 9750001),
    (9750204, 0, now(), NULL, NULL, NULL, '删除附件', '/rc-consultation-attachment/{id}', 'DELETE', 9750001),

    (9750301, 0, now(), NULL, NULL, NULL, '成员分页', '/rc-consultation-member', 'GET', 9750001),
    (9750302, 0, now(), NULL, NULL, NULL, '新增成员', '/rc-consultation-member', 'POST', 9750001),
    (9750303, 0, now(), NULL, NULL, NULL, '更新成员', '/rc-consultation-member/{id}', 'PUT', 9750001),
    (9750304, 0, now(), NULL, NULL, NULL, '删除成员', '/rc-consultation-member/{id}', 'DELETE', 9750001),

    (9750401, 0, now(), NULL, NULL, NULL, '时间线分页', '/rc-consultation-timeline', 'GET', 9750001),

    (9750501, 0, now(), NULL, NULL, NULL, '报告分页', '/rc-consultation-report', 'GET', 9750001),
    (9750502, 0, now(), NULL, NULL, NULL, '报告详情', '/rc-consultation-report/{id}', 'GET', 9750001),
    (9750503, 0, now(), NULL, NULL, NULL, '新增报告', '/rc-consultation-report', 'POST', 9750001),
    (9750504, 0, now(), NULL, NULL, NULL, '更新报告', '/rc-consultation-report/{id}', 'PUT', 9750001),
    (9750505, 0, now(), NULL, NULL, NULL, '签署报告', '/rc-consultation-report/sign', 'POST', 9750001),
    (9750506, 0, now(), NULL, NULL, NULL, '删除报告', '/rc-consultation-report/{id}', 'DELETE', 9750001),

    (9750601, 0, now(), NULL, NULL, NULL, '患者分页', '/rc-patient-case', 'GET', 9750001),
    (9750602, 0, now(), NULL, NULL, NULL, '患者详情', '/rc-patient-case/{id}', 'GET', 9750001),
    (9750603, 0, now(), NULL, NULL, NULL, '新增患者', '/rc-patient-case', 'POST', 9750001),
    (9750604, 0, now(), NULL, NULL, NULL, '更新患者', '/rc-patient-case/{id}', 'PUT', 9750001),
    (9750605, 0, now(), NULL, NULL, NULL, '删除患者', '/rc-patient-case/{id}', 'DELETE', 9750001),

    (9750701, 0, now(), NULL, NULL, NULL, '资料包分页', '/rc-patient-pack', 'GET', 9750001),
    (9750702, 0, now(), NULL, NULL, NULL, '资料包详情', '/rc-patient-pack/{id}', 'GET', 9750001),
    (9750703, 0, now(), NULL, NULL, NULL, '新增资料包', '/rc-patient-pack', 'POST', 9750001),
    (9750704, 0, now(), NULL, NULL, NULL, '更新资料包', '/rc-patient-pack/{id}', 'PUT', 9750001),
    (9750705, 0, now(), NULL, NULL, NULL, '删除资料包', '/rc-patient-pack/{id}', 'DELETE', 9750001),

    (9750801, 0, now(), NULL, NULL, NULL, '同步任务分页', '/rc-sync-task', 'GET', 9750001),
    (9750802, 0, now(), NULL, NULL, NULL, '同步任务详情', '/rc-sync-task/{id}', 'GET', 9750001),
    (9750803, 0, now(), NULL, NULL, NULL, '新增同步任务', '/rc-sync-task', 'POST', 9750001),
    (9750804, 0, now(), NULL, NULL, NULL, '更新同步任务', '/rc-sync-task/{id}', 'PUT', 9750001),
    (9750805, 0, now(), NULL, NULL, NULL, '删除同步任务', '/rc-sync-task/{id}', 'DELETE', 9750001)
ON CONFLICT (id)
DO UPDATE SET name = EXCLUDED.name, path = EXCLUDED.path, request_method = EXCLUDED.request_method, group_id = EXCLUDED.group_id;

-- 4. 菜单与接口绑定
INSERT INTO purest_function_interface (id, create_by, create_time, update_by, update_time, remark, interface_id, function_id)
VALUES
    (9760001, 0, now(), NULL, NULL, NULL, 9750101, 9700104),
    (9760002, 0, now(), NULL, NULL, NULL, 9750102, 9700104),
    (9760003, 0, now(), NULL, NULL, NULL, 9750103, 9700102),
    (9760004, 0, now(), NULL, NULL, NULL, 9750104, 9700103),
    (9760005, 0, now(), NULL, NULL, NULL, 9750105, 9700105),
    (9760006, 0, now(), NULL, NULL, NULL, 9750106, 9700106),
    (9760007, 0, now(), NULL, NULL, NULL, 9750107, 9700107),
    (9760008, 0, now(), NULL, NULL, NULL, 9750108, 9700111),
    (9760009, 0, now(), NULL, NULL, NULL, 9750109, 9700108),
    (9760010, 0, now(), NULL, NULL, NULL, 9750110, 9700109),
    (9760011, 0, now(), NULL, NULL, NULL, 9750111, 9700110),

    (9760012, 0, now(), NULL, NULL, NULL, 9750201, 9700114),
    (9760013, 0, now(), NULL, NULL, NULL, 9750202, 9700114),
    (9760014, 0, now(), NULL, NULL, NULL, 9750203, 9700114),
    (9760015, 0, now(), NULL, NULL, NULL, 9750204, 9700114),

    (9760016, 0, now(), NULL, NULL, NULL, 9750301, 9700113),
    (9760017, 0, now(), NULL, NULL, NULL, 9750302, 9700113),
    (9760018, 0, now(), NULL, NULL, NULL, 9750303, 9700113),
    (9760019, 0, now(), NULL, NULL, NULL, 9750304, 9700113),

    (9760020, 0, now(), NULL, NULL, NULL, 9750401, 9700104),

    (9760021, 0, now(), NULL, NULL, NULL, 9750501, 9700112),
    (9760022, 0, now(), NULL, NULL, NULL, 9750502, 9700112),
    (9760023, 0, now(), NULL, NULL, NULL, 9750503, 9700112),
    (9760024, 0, now(), NULL, NULL, NULL, 9750504, 9700112),
    (9760025, 0, now(), NULL, NULL, NULL, 9750505, 9700112),
    (9760026, 0, now(), NULL, NULL, NULL, 9750506, 9700112),

    (9760027, 0, now(), NULL, NULL, NULL, 9750601, 9700204),
    (9760028, 0, now(), NULL, NULL, NULL, 9750602, 9700204),
    (9760029, 0, now(), NULL, NULL, NULL, 9750603, 9700202),
    (9760030, 0, now(), NULL, NULL, NULL, 9750604, 9700203),
    (9760031, 0, now(), NULL, NULL, NULL, 9750605, 9700205),

    (9760032, 0, now(), NULL, NULL, NULL, 9750701, 9700304),
    (9760033, 0, now(), NULL, NULL, NULL, 9750702, 9700304),
    (9760034, 0, now(), NULL, NULL, NULL, 9750703, 9700302),
    (9760035, 0, now(), NULL, NULL, NULL, 9750704, 9700303),
    (9760036, 0, now(), NULL, NULL, NULL, 9750705, 9700305),

    (9760037, 0, now(), NULL, NULL, NULL, 9750801, 9700404),
    (9760038, 0, now(), NULL, NULL, NULL, 9750802, 9700404),
    (9760039, 0, now(), NULL, NULL, NULL, 9750803, 9700402),
    (9760040, 0, now(), NULL, NULL, NULL, 9750804, 9700403),
    (9760041, 0, now(), NULL, NULL, NULL, 9750805, 9700405)
ON CONFLICT (id)
DO UPDATE SET interface_id = EXCLUDED.interface_id, function_id = EXCLUDED.function_id;

COMMIT;

INSERT INTO purest_function (id, create_by, create_time, update_by, update_time, remark, name, code, parent_id) VALUES (9700501, 0, now(), NULL, NULL, '远程会诊-音视频演示菜单', '音视频演示', 'remote.rtcDemo', 9700001) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, parent_id = EXCLUDED.parent_id, remark = EXCLUDED.remark;

INSERT INTO purest_function (id, create_by, create_time, update_by, update_time, remark, name, code, parent_id) VALUES (9700502, 0, now(), NULL, NULL, NULL, '查看演示', 'remote.rtcDemo.view', 9700501) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, parent_id = EXCLUDED.parent_id, remark = EXCLUDED.remark;