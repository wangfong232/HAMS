using System;
using System.Linq;
using HAMS.Domain.Entities;

namespace HAMS.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(HamsDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Departments.Any())
            {
                EnsureTodaySlots(context);
                return; // DB already seeded
            }

            // 1. Department & Room
            var dept = new Department
            {
                Code = "K_NOI",
                Name = "Khoa Nội tổng quát",
                BaseConsultationFee = 150000m,
                Description = "Khám và điều trị các bệnh lý nội tổng quát, tim mạch, hô hấp"
            };
            context.Departments.Add(dept);

            var room = new Room
            {
                RoomNumber = "P201",
                Department = dept,
                RoomType = "Consultation",
                IsOperational = true
            };
            context.Rooms.Add(room);

            // 2. Doctor User & Profile
            var docUser = new User
            {
                FullName = "BS.CKII Trần Quang Phong",
                Email = "phongtq@hams.vn",
                PhoneNumber = "0912345678",
                Role = "Doctor"
            };
            context.Users.Add(docUser);

            var doctor = new Doctor
            {
                User = docUser,
                Department = dept,
                AcademicTitle = "BS.CKII",
                Specialization = "Nội tổng quát & Hô hấp",
                PricePerSlot = 150000m
            };
            context.Doctors.Add(doctor);

            // 3. Receptionist User
            var receptionUser = new User
            {
                FullName = "Nguyễn Hữu Minh Đức",
                Email = "ducnhm@hams.vn",
                PhoneNumber = "0923456789",
                Role = "Receptionist"
            };
            context.Users.Add(receptionUser);

            // 4. Patient User & Profile
            var patientUser = new User
            {
                FullName = "Nguyễn Văn An",
                Email = "patient.an@gmail.com",
                PhoneNumber = "0988776655",
                Role = "Patient"
            };
            context.Users.Add(patientUser);

            var patient = new Patient
            {
                User = patientUser,
                DateOfBirth = new DateTime(1995, 5, 20),
                Gender = "Nam",
                Address = "Hà Nội"
            };
            context.Patients.Add(patient);

            // 5. Medicines
            context.Medicines.AddRange(
                new Medicine
                {
                    Name = "Paracetamol 500mg",
                    Unit = "Viên",
                    Price = 2500m,
                    IsAvailable = true
                },
                new Medicine
                {
                    Name = "Amoxicillin 500mg",
                    Unit = "Viên",
                    Price = 5000m,
                    IsAvailable = true
                },
                new Medicine
                {
                    Name = "Cetirizine 10mg",
                    Unit = "Viên",
                    Price = 3000m,
                    IsAvailable = true
                }
            );

            context.SaveChanges();

            // 6. Ensure Morning & Afternoon schedules and slots for today
            EnsureTodaySlots(context);
        }

        public static void EnsureTodaySlots(HamsDbContext context)
        {
            var today = DateTime.Today;
            var doc = context.Doctors.FirstOrDefault();
            var room = context.Rooms.FirstOrDefault();
            if (doc == null || room == null) return;

            // Check if morning schedule exists for today
            var hasMorning = context.DoctorSchedules.Any(s => s.DoctorId == doc.Id && s.ShiftDate == today && s.StartTime < new TimeSpan(12, 0, 0));
            if (!hasMorning)
            {
                var amSched = new DoctorSchedule
                {
                    Doctor = doc,
                    Room = room,
                    ShiftDate = today,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    MaxPatients = 16
                };
                context.DoctorSchedules.Add(amSched);

                var amStart = today.AddHours(8);
                for (int i = 0; i < 16; i++)
                {
                    var slotTime = amStart.AddMinutes(i * 15);
                    context.TimeSlots.Add(new TimeSlot
                    {
                        Schedule = amSched,
                        Doctor = doc,
                        SlotTime = slotTime,
                        EndTime = slotTime.AddMinutes(15),
                        Status = "Open",
                        RowVersion = new byte[] { (byte)(i + 1), 0, 0, 0 }
                    });
                }
            }

            // Check if afternoon schedule exists for today
            var hasAfternoon = context.DoctorSchedules.Any(s => s.DoctorId == doc.Id && s.ShiftDate == today && s.StartTime >= new TimeSpan(12, 0, 0));
            if (!hasAfternoon)
            {
                var pmSched = new DoctorSchedule
                {
                    Doctor = doc,
                    Room = room,
                    ShiftDate = today,
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    MaxPatients = 16
                };
                context.DoctorSchedules.Add(pmSched);

                var pmStart = today.AddHours(13);
                for (int i = 0; i < 16; i++)
                {
                    var slotTime = pmStart.AddMinutes(i * 15);
                    context.TimeSlots.Add(new TimeSlot
                    {
                        Schedule = pmSched,
                        Doctor = doc,
                        SlotTime = slotTime,
                        EndTime = slotTime.AddMinutes(15),
                        Status = "Open",
                        RowVersion = new byte[] { (byte)(i + 17), 0, 0, 0 }
                    });
                }
            }

            context.SaveChanges();
        }

        public static void ResetDemoData(HamsDbContext context)
        {
            // 1. Remove all prescription details
            var prescriptions = context.PrescriptionDetails.ToList();
            if (prescriptions.Any()) context.PrescriptionDetails.RemoveRange(prescriptions);

            // 2. Remove all medical records
            var records = context.MedicalRecords.ToList();
            if (records.Any()) context.MedicalRecords.RemoveRange(records);

            // 3. Remove all vital signs
            var vitals = context.VitalSigns.ToList();
            if (vitals.Any()) context.VitalSigns.RemoveRange(vitals);

            // 4. Remove all payments
            var payments = context.Payments.ToList();
            if (payments.Any()) context.Payments.RemoveRange(payments);

            // 5. Remove all appointments
            var appointments = context.Appointments.ToList();
            if (appointments.Any()) context.Appointments.RemoveRange(appointments);

            context.SaveChanges();

            // 6. Ensure today's schedules and slots exist
            EnsureTodaySlots(context);

            // 7. Reset all slots for today to Open
            var today = DateTime.Today;
            var nextDay = today.AddDays(1);
            var slots = context.TimeSlots
                .Where(s => s.SlotTime >= today && s.SlotTime < nextDay)
                .ToList();

            foreach (var slot in slots)
            {
                slot.Status = "Open";
                slot.HeldByUserId = null;
                slot.HoldExpiresAt = null;
                slot.UpdatedAt = DateTime.UtcNow;
            }

            context.SaveChanges();
        }
    }
}
