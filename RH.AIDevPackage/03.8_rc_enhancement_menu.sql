-- 03.8_rc_enhancement_menu.sql
-- 修复：统一 ID 体系，确保新菜单权限分配给 990... 系列角色

BEGIN;

-- 1. 新增菜单功能点 (依赖 9700001 - 远程会诊根节点)
INSERT INTO purest_function (id, create_by, create_time, update_by, update_time, remark, name, code, parent_id)
VALUES
    (9700501, 0, now(), NULL, NULL, '远程会诊-会诊驾驶舱', '会诊驾驶舱', 'remote.dashboard', 9700001),
    (9700601, 0, now(), NULL, NULL, '远程会诊-排期中心', '排期中心', 'remote.calendar', 9700001),
    (9700701, 0, now(), NULL, NULL, '远程会诊-评价管理', '评价查看', 'remote.evaluation.view', 9700001)
ON CONFLICT (id) 
DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code, parent_id = EXCLUDED.parent_id, remark = EXCLUDED.remark;

-- 2. 新增接口授权 (依赖 9750001 - 远程会诊接口组)
INSERT INTO purest_interface (id, create_by, create_time, update_by, update_time, remark, name, path, request_method, group_id)
VALUES
    (9760501, 0, now(), NULL, NULL, '获取会诊统计趋势', '会诊趋势', '/api/v1/rc-consultation/trend', 'GET', 9750001),
    (9760502, 0, now(), NULL, NULL, '获取会诊分布统计', '会诊分布', '/api/v1/rc-consultation/distribution', 'GET', 9750001),
    (9760503, 0, now(), NULL, NULL, '获取专家响应排行', '专家排行', '/api/v1/rc-consultation/expert-ranking', 'GET', 9750001),
    (9760701, 0, now(), NULL, NULL, '查询评价列表', '评价列表', '/api/v1/rc-consultation-evaluation', 'GET', 9750001),
    (9760702, 0, now(), NULL, NULL, '提交评价(H5用)', '提交评价', '/api/v1/rc-consultation-evaluation', 'POST', 9750001)
ON CONFLICT (id) DO NOTHING;

-- 3. 关联功能与接口
INSERT INTO purest_function_interface (id, create_by, create_time, update_by, update_time, remark, interface_id, function_id)
VALUES
    (9770501, 0, now(), NULL, NULL, NULL, 9760501, 9700501),
    (9770502, 0, now(), NULL, NULL, NULL, 9760502, 9700501),
    (9770503, 0, now(), NULL, NULL, NULL, 9760503, 9700501),
    (9770701, 0, now(), NULL, NULL, NULL, 9760701, 9700701)
ON CONFLICT (id) DO NOTHING;

-- 4. 自动为演示账号(Host)分配新权限
-- 使用统一的演示主持人角色 ID: 9900000200001
INSERT INTO purest_role_function (id, create_by, create_time, role_id, function_id)
SELECT (400000 + id), 0, now(), 9900000200001, id FROM purest_function 
WHERE code IN ('remote.dashboard', 'remote.calendar', 'remote.evaluation.view')
ON CONFLICT (id) DO NOTHING;

COMMIT;
