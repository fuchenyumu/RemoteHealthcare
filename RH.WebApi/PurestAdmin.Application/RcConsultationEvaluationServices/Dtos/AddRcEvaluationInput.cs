namespace PurestAdmin.Application.RcConsultationEvaluationServices.Dtos;

public class AddRcEvaluationInput
{
    public long ConsultationId { get; set; }

    public string PatientName { get; set; }

    public int Score { get; set; }

    public string Tags { get; set; }

    public string Content { get; set; }

    public bool IsAnonymous { get; set; }
}






