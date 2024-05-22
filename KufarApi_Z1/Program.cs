using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KufarWebApi;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static async Task Main(string[] args)
    {
        string url = "https://api.kufar.by/search-api/v2/search/rendered-paginated?cat=1010&cur=USD&gtsy=country-belarus~province-minsk~locality-minsk&lang=ru&size=30&typ=sell";
        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        JObject data = JObject.Parse(jsonResponse);
        var apartments = data["ads"];

        List<Apartment> apartmentList = new List<Apartment>();

        foreach (var apartment in apartments)
        {
            double price = 0, pricePerSquareMeter = 0, area = 0;
            int floor = 0, rooms = 0;
            string metro = "Не указано";

            // Получаем цены
            if (apartment["price_usd"] != null)
            {
                price = apartment["price_usd"].Value<double>();
            }
            else
            {
                continue; // Пропускаем, если цена отсутствует
            }

            // Получаем параметры из массивов
            var attributes = apartment["ad_parameters"] as JArray;
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    string parameter = attribute["p"]?.ToString();
                    if (parameter == "square_meter" && attribute["v"]?.Type != null)
                    {
                        pricePerSquareMeter = attribute["v"].Value<double>();
                    }
                    else if (parameter == "floor" && attribute["v"]?.Type == JTokenType.Array)
                    {
                        floor = attribute["v"].First.Value<int>();
                    }
                    else if (parameter == "rooms" && attribute["v"]?.Type == JTokenType.String)
                    {
                        rooms = int.Parse(attribute["v"].ToString());
                    }
                    else if (parameter == "metro" && attribute["v"]?.Type == JTokenType.Array)
                    {
                        metro = attribute["v"].First.ToString();
                    }
                    else if (parameter == "size" && attribute["v"]?.Type != null)
                    {
                        area = attribute["v"].Value<double>();
                    }
                }
            }

            if (price < 10000) continue; // Игнорируем квартиры с низкой стоимостью

            apartmentList.Add(new Apartment
            {
                FullPrice = price,
                Area = area,
                PricePerSquareMeter = pricePerSquareMeter,
                Floor = floor,
                Rooms = rooms,
                Metro = metro
            });
        }


        AnalyzeData(apartmentList);
    }

    static void AnalyzeData(List<Apartment> apartments)
    {
        Console.WriteLine("Анализ стоимости за квадратный метр:");

        // Квадратный метр от этажа 
        var floorGroups = GroupBy(apartments, a => a.Floor);
        PrintGroupAnalysis(floorGroups, "этаж");

        // Квадратный метр от колличества комнат
        var roomGroups = GroupBy(apartments, a => a.Rooms);
        PrintGroupAnalysis(roomGroups, "количество комнат");

        // Станция метро
        var metroGroups = GroupBy(apartments, a => a.Metro);
        PrintGroupAnalysis(metroGroups, "станция метро");
    }
    //метод группировки
    static Dictionary<TKey, List<Apartment>> GroupBy<TKey>(List<Apartment> apartments, Func<Apartment, TKey> keySelector)
    {
        Dictionary<TKey, List<Apartment>> groups = new Dictionary<TKey, List<Apartment>>();

        foreach (var apartment in apartments)
        {
            TKey key = keySelector(apartment);
            if (!groups.ContainsKey(key))
            {
                groups[key] = new List<Apartment>();
            }
            groups[key].Add(apartment);
        }

        return groups;
    }
    //Выводим результаты в консоль 
    static void PrintGroupAnalysis<TKey>(Dictionary<TKey, List<Apartment>> groups, string groupName)
    {
        Console.WriteLine($"\nЗависимость от {groupName}:");

        foreach (var group in groups)
        {
            double averagePricePerSquareMeter = 0;
            foreach (var apartment in group.Value)
            {
                averagePricePerSquareMeter += apartment.PricePerSquareMeter;
            }
            averagePricePerSquareMeter /= group.Value.Count;
            Console.WriteLine($"{groupName}: {group.Key}, средняя стоимость за метр квадратный: {averagePricePerSquareMeter:F2} USD");
        }
    }
}
