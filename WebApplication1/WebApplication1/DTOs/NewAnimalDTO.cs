﻿namespace WebApplication1.DTOs;



public class NewAnimalDTO
{
    public string Name { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public int OwnerId { get; set; }
}