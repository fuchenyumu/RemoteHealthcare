
using System.ComponentModel.DataAnnotations;

namespace PurestAdmin.Application.RcPatientCaseServices.Dtos;

public class GetPagedListInput : PaginationParams
{
    public string Keyword { get; set; }

    [MaxLength(20)]
    public string PatientType { get; set; }

    [MaxLength(30)]
    public string DataSource { get; set; }
}
