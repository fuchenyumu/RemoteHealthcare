import request from '@/utils/request'

export function submitEvaluation(data: any) {
  return request({
    url: '/rc-consultation-evaluation',
    method: 'post',
    data
  })
}

export function getConsultation(id: string) {
  return request({
    url: `/rc-consultation/${id}`,
    method: 'get'
  })
}

