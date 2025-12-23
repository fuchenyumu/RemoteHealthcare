import type { MockMethod } from "vite-plugin-fake-server";

let instanceIdSeq = 5000;
const instances: any[] = [];

export default [
  {
    url: "/api/v1/definition/definitions",
    method: "get",
    response: () => {
      return { data: [{ id: 1, name: "ConsultationWorkflow" }] };
    }
  },
  {
    url: "/api/v1/instance/:id/start",
    method: "post",
    response: ({ url, body }) => {
      const definitionId = Number(url.split("/")[2]);
      const id = ++instanceIdSeq;
      instances.push({ id, definitionId, ...body, status: "RUNNING" });
      return { data: { instanceId: id } };
    }
  },
  {
    url: "/api/v1/instance/:id/auditing",
    method: "post",
    response: ({ url, body }) => {
      const id = Number(url.split("/")[2]);
      const inst = instances.find(x => x.id === id);
      if (inst) inst.lastAudit = body;
      return { data: true };
    }
  },
  {
    url: "/api/v1/instance/:id/instance-detail",
    method: "get",
    response: ({ url }) => {
      const id = Number(url.split("/")[2]);
      const inst = instances.find(x => x.id === id) || null;
      return { data: inst };
    }
  }
] as MockMethod[];
