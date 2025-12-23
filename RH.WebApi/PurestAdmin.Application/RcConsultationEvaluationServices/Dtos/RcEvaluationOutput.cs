namespace PurestAdmin.Application.RcConsultationEvaluationServices.Dtos;

public class RcEvaluationOutput
{
    public long Id { get; set; }

    public long ConsultationId { get; set; }

    public string PatientName { get; set; }

    public int Score { get; set; }

    public string Tags { get; set; }

    public string Content { get; set; }

    public bool IsAnonymous { get; set; }

    public DateTime CreateTime { get; set; }
}






