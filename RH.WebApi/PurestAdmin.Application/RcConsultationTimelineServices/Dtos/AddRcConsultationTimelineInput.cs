
namespace PurestAdmin.Application.RcConsultationTimelineServices.Dtos;
public class AddRcConsultationTimelineInput
{
	/// <summary>
	/// 
	/// </summary>
	public string Remark { get; set; }
	/// <summary>
	/// 
	/// </summary>
	[Required(ErrorMessage = "不能为空"), MaxLength(0, ErrorMessage = "最大长度为：0")]
	public string ConsultationId { get; set; }
	/// <summary>
	/// 
	/// </summary>
	[Required(ErrorMessage = "不能为空"), MaxLength(30, ErrorMessage = "最大长度为：30")]
	public string EventCode { get; set; }
	/// <summary>
	/// 
	/// </summary>
	public string EventContent { get; set; }
	/// <summary>
	/// 
	/// </summary>
	public string Snapshot { get; set; }
}
