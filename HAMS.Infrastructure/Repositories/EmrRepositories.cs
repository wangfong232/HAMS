using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HAMS.Domain.Entities;
using HAMS.Application.Interfaces;
using HAMS.Infrastructure.Data;
public class EmrRepository : IEmrRepository
 {
     private readonly HamsDbContext _context;

     public EmrRepository(HamsDbContext context)
     {
         _context = context;
     }

     public async Task AddMedicalRecordAsync(MedicalRecord medicalRecord)
     {
         await _context.MedicalRecords.AddAsync(medicalRecord);
         await _context.SaveChangesAsync();
     }

     public async Task AddPrescriptionDetailAsync(PrescriptionDetail detail)
     {
         await _context.PrescriptionDetails.AddAsync(detail);
         await _context.SaveChangesAsync();
     }
 }

 public class MedicineRepository : IMedicineRepository
 {
     private readonly HamsDbContext _context;

     public MedicineRepository(HamsDbContext context)
     {
         _context = context;
     }

     public async Task<Medicine?> GetByIdAsync(int medicineId)
     {
         return await _context.Medicines.FindAsync(medicineId);
     }

     public async Task<IEnumerable<Medicine>> GetAvailableMedicinesAsync(string keyword)
     {
         var query = _context.Medicines.Where(m => m.IsAvailable);
         if (!string.IsNullOrWhiteSpace(keyword))
         {
             query = query.Where(m => m.Name.Contains(keyword));
         }
         return await query.ToListAsync();
     }
 }
