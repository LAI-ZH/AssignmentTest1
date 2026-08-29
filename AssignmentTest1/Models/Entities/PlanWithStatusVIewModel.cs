using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Models.ViewModels
{
    public class PlanWithStatsViewModel
    {
        public MembershipPlan Plan { get; set; }
        public int ActiveCount { get; set; }
        public int UniqueUserCount { get; set; }  // ✅ 去重后的用户数
        public int TotalSubscriptions { get; set; } // 总订阅记录数（可选）
    }
}