using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using GUI.DTO.models;

public static class PrizeWinnerStore
{
    public static List<VoucherDTO> Winners = new List<VoucherDTO>();

    public static void SaveWinners(string filePath)
    {
        var json = JsonConvert.SerializeObject(Winners);
        File.WriteAllText(filePath, json);
    }

    public static void LoadWinners(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            Winners = JsonConvert.DeserializeObject<List<VoucherDTO>>(json) ?? new List<VoucherDTO>();
        }
        else
        {
            Winners = new List<VoucherDTO>();
        }
    }
}