﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Models
{
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public string Id { get; set; }

        [Required]
        public virtual Robot Robot { get; set; }

        [MaxLength(128)]
        [Required]
        public string IsarMissionId { get; set; }

        [MaxLength(128)]
        [Required]
        public string EchoMissionId { get; set; }

        [MaxLength(128)]
        [Required]
        public string Log { get; set; }

        [Required]
        public ReportStatus ReportStatus { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        [Required]
        public virtual IList<IsarTask> Tasks { get; set; }
    }

    public enum ReportStatus
    {
        Successful,
        NotStarted,
        InProgress,
        Failed,
        Cancelled,
    }

    public static class ReportStatusMethods
    {
        public static ReportStatus FromString(string status) =>
            status switch
            {
                "completed" => ReportStatus.Successful,
                "not_started" => ReportStatus.NotStarted,
                "in_progress" => ReportStatus.InProgress,
                "failed" => ReportStatus.Failed,
                "cancelled" => ReportStatus.Cancelled,
                _
                  => throw new ArgumentException(
                      $"Failed to parse report status {status} as it's not supported"
                  )
            };
    }
}
