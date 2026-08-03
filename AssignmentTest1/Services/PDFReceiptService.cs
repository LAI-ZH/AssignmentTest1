using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Services
{
    public class PDFReceiptService
    {
        public byte[] GeneratePaymentReceipt(Payment payment, User user, MembershipPlan plan, DateTime? expiryDate = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // 如果没有传入 expiryDate，默认计算
            if (expiryDate == null)
            {
                expiryDate = DateTime.Now.AddDays(plan.DurationDays);
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // ===== HEADER =====
                    page.Header()
                        .Column(col =>
                        {
                            col.Item().Text("FitBook - Payment Receipt")
                                .FontSize(22)
                                .Bold()
                                .FontColor("#4F6EF7")
                                .AlignCenter();

                            col.Item().PaddingTop(5)
                                .Text($"Receipt Date: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}")
                                .FontSize(12)
                                .AlignCenter()
                                .FontColor(Colors.Grey.Medium);

                            col.Item().PaddingTop(10)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten1);
                        });

                    // ===== CONTENT =====
                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            // Receipt Information
                            col.Item().Text("Receipt Information")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1A1A2E");

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Receipt ID:");
                                row.RelativeItem().Text(payment.TransactionId ?? "N/A");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Payment Date:");
                                row.RelativeItem().Text(payment.PaymentDate.ToString("dd/MM/yyyy HH:mm"));
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Payment Method:");
                                row.RelativeItem().Text(payment.PaymentMethod);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Status:");
                                row.RelativeItem().Text(payment.Status)
                                    .FontColor(Colors.Green.Medium)
                                    .Bold();
                            });

                            col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            // User Information
                            col.Item().PaddingTop(10).Text("User Information")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1A1A2E");

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Name:");
                                row.RelativeItem().Text(user.FullName);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Email:");
                                row.RelativeItem().Text(user.Email);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Phone:");
                                row.RelativeItem().Text(user.Phone ?? "-");
                            });

                            col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            // Plan Details
                            col.Item().PaddingTop(10).Text("Plan Details")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1A1A2E");

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Plan Name:");
                                row.RelativeItem().Text(plan.PlanName);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Duration:");
                                row.RelativeItem().Text($"{plan.DurationDays} days");
                            });

                            // ✅ 新增：Expiry Date
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Expiry Date:")
                                    .Bold()
                                    .FontColor("#DC3545");
                                row.RelativeItem().Text(expiryDate.Value.ToString("dd/MM/yyyy"))
                                    .Bold()
                                    .FontColor("#DC3545");
                            });

                            if (plan.MaxBookings == 0)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Classes:");
                                    row.RelativeItem().Text("Unlimited");
                                });
                            }
                            else
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Classes:");
                                    row.RelativeItem().Text($"{plan.MaxBookings} classes");
                                });
                            }

                            if (plan.PTSessions > 0)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("PT Sessions:");
                                    row.RelativeItem().Text($"{plan.PTSessions} sessions");
                                });
                            }

                            col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            // Payment Summary
                            col.Item().PaddingTop(10).Text("Payment Summary")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1A1A2E");

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Subtotal:");
                                row.RelativeItem().Text($"RM {plan.Price:F2}");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Tax (0%):");
                                row.RelativeItem().Text("RM 0.00");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Total Amount:")
                                    .Bold()
                                    .FontColor("#1A1A2E");
                                row.RelativeItem().Text($"RM {plan.Price:F2}")
                                    .Bold()
                                    .FontColor("#198754");
                            });
                        });

                    // ===== FOOTER =====
                    page.Footer()
                        .Column(col =>
                        {
                            col.Item().AlignCenter()
                                .Text("Thank you for choosing FitBook!")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Medium);

                            col.Item().PaddingTop(5)
                                .AlignCenter()
                                .Text("www.fitbook.com | support@fitbook.com")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Lighten1);
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateBookingReceipt(Booking booking, User member, FitnessClass fitnessClass, ClassSchedule schedule)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // ===== HEADER =====
                    page.Header()
                        .Column(col =>
                        {
                            col.Item().Text("FitBook - Booking Confirmation")
                                .FontSize(22)
                                .Bold()
                                .FontColor("#4F6EF7")
                                .AlignCenter();

                            col.Item().PaddingTop(5)
                                .Text($"Booking Date: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}")
                                .FontSize(12)
                                .AlignCenter()
                                .FontColor(Colors.Grey.Medium);

                            col.Item().PaddingTop(10)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten1);
                        });

                    // ===== CONTENT =====
                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            col.Item().Text("Booking Details")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1A1A2E");

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Booking ID:");
                                row.RelativeItem().Text($"#{booking.BookingId}");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Class:");
                                row.RelativeItem().Text(fitnessClass.ClassName);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Category:");
                                row.RelativeItem().Text(fitnessClass.Category);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Date:");
                                row.RelativeItem().Text(schedule.ScheduleDate.ToString("dd/MM/yyyy"));
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Time:");
                                row.RelativeItem().Text($"{schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Venue:");
                                row.RelativeItem().Text(schedule.Venue);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Trainer:");
                                row.RelativeItem().Text(fitnessClass.Trainer?.FullName ?? "TBD");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Status:");
                                row.RelativeItem().Text(booking.Status)
                                    .FontColor(booking.Status == "Confirmed" ? Colors.Green.Medium : Colors.Orange.Medium)
                                    .Bold();
                            });

                            col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            col.Item().PaddingTop(10).Text("Member Information")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#1A1A2E");

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Name:");
                                row.RelativeItem().Text(member.FullName);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Email:");
                                row.RelativeItem().Text(member.Email);
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Phone:");
                                row.RelativeItem().Text(member.Phone ?? "-");
                            });
                        });

                    // ===== FOOTER =====
                    page.Footer()
                        .Column(col =>
                        {
                            col.Item().AlignCenter()
                                .Text("Thank you for booking with FitBook!")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Medium);

                            col.Item().PaddingTop(5)
                                .AlignCenter()
                                .Text("www.fitbook.com | support@fitbook.com")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Lighten1);
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}