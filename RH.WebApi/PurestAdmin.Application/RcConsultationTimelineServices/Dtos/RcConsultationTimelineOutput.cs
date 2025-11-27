
namespace PurestAdmin.Application.RcConsultationTimelineServices.Dtos;

public class RcConsultationTimelineOutput
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public string EventCode { get; set; }

    public string EventCodeLabel { get; set; }

    public string EventContent { get; set; }

    public string Snapshot { get; set; }

    public DateTime CreateTime { get; set; }

    public long CreateBy { get; set; }

    public string CreateByName { get; set; }
}
