using Microsoft.EntityFrameworkCore;
using MojRok.Domain.Entities;

namespace MojRok.Infrastructure.Persistence;

/// <summary>
/// Seeds Municipalities, ServiceCategories and ~25 real Macedonian citizen services.
/// Idempotent - safe to call on every startup.
/// </summary>
public static class CitizenServicesSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedMunicipalitiesAsync(db);
        await SeedServiceCategoriesAndServicesAsync(db);
    }

    private static async Task SeedMunicipalitiesAsync(AppDbContext db)
    {
        if (await db.Municipalities.AnyAsync())
            return;

        var municipalities = new[]
        {
            new Municipality { Name = "Skopje",   NameMk = "Скопје",   Region = "Skopje" },
            new Municipality { Name = "Bitola",   NameMk = "Битола",   Region = "Pelagonia" },
            new Municipality { Name = "Kumanovo", NameMk = "Куманово", Region = "Northeast" },
            new Municipality { Name = "Prilep",   NameMk = "Прилеп",   Region = "Pelagonia" },
            new Municipality { Name = "Tetovo",   NameMk = "Тетово",   Region = "Polog" },
            new Municipality { Name = "Veles",    NameMk = "Велес",    Region = "Vardar" },
            new Municipality { Name = "Stip",     NameMk = "Штип",     Region = "East" },
            new Municipality { Name = "Ohrid",    NameMk = "Охрид",    Region = "Southwest" },
            new Municipality { Name = "Gostivar", NameMk = "Гостивар", Region = "Polog" },
            new Municipality { Name = "Strumica", NameMk = "Струмица", Region = "Southeast" },
        };

        db.Municipalities.AddRange(municipalities);
        await db.SaveChangesAsync();
    }

    private static async Task SeedServiceCategoriesAndServicesAsync(AppDbContext db)
    {
        if (await db.ServiceCategories.AnyAsync())
            return;

        var catDocs = new ServiceCategory { Name = "Personal Documents", NameMk = "Лични документи", IconSlug = "id-card", SortOrder = 1 };
        var catCivil = new ServiceCategory { Name = "Civil Registry", NameMk = "Матични книги", IconSlug = "book", SortOrder = 2 };
        var catVehicle = new ServiceCategory { Name = "Vehicles", NameMk = "Возила", IconSlug = "car", SortOrder = 3 };
        var catTax = new ServiceCategory { Name = "Taxes", NameMk = "Даноци", IconSlug = "receipt", SortOrder = 4 };
        var catSocial = new ServiceCategory { Name = "Social Protection", NameMk = "Социјална заштита", IconSlug = "heart", SortOrder = 5 };
        var catHealth = new ServiceCategory { Name = "Health", NameMk = "Здравство", IconSlug = "medical", SortOrder = 6 };
        var catBusiness = new ServiceCategory { Name = "Business", NameMk = "Бизнис", IconSlug = "briefcase", SortOrder = 7 };
        var catProperty = new ServiceCategory { Name = "Property", NameMk = "Имот", IconSlug = "home", SortOrder = 8 };

        db.ServiceCategories.AddRange(catDocs, catCivil, catVehicle, catTax, catSocial, catHealth, catBusiness, catProperty);
        await db.SaveChangesAsync();

        var skopje = await db.Municipalities.FirstAsync(m => m.Name == "Skopje");
        var bitola = await db.Municipalities.FirstAsync(m => m.Name == "Bitola");
        var tetovo = await db.Municipalities.FirstAsync(m => m.Name == "Tetovo");

        var services = new List<CitizenService>
        {
            new() {
                Name = "Issuing ID Card", NameMk = "Издавање лична карта",
                Description = "First issue or replacement of national ID card",
                DescriptionMk = "Прво издавање или замена на лична карта",
                ServiceCategoryId = catDocs.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.uv.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Issuing Passport", NameMk = "Издавање пасош",
                Description = "Biometric passport for citizens of North Macedonia",
                DescriptionMk = "Биометриски пасош за граѓани на РСМ",
                ServiceCategoryId = catDocs.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.uv.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Issuing Driving Licence", NameMk = "Издавање возачка дозвола",
                Description = "First driving licence or renewal",
                DescriptionMk = "Прва возачка дозвола или продолжување",
                ServiceCategoryId = catDocs.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.uv.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Change of Personal Data", NameMk = "Промена на лични податоци",
                Description = "Change of name or other data on ID card",
                DescriptionMk = "Промена на име, презиме или други податоци во лична карта",
                ServiceCategoryId = catDocs.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.uv.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Birth Certificate", NameMk = "Извод од матична книга на родените",
                Description = "Extract from the birth register",
                DescriptionMk = "Извод за раѓање",
                ServiceCategoryId = catCivil.Id, MunicipalityId = null,
                WebsiteUrl = "https://uslugi.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Marriage Certificate", NameMk = "Извод од матична книга на венчаните",
                Description = "Extract from the marriage register",
                DescriptionMk = "Извод за склучен брак",
                ServiceCategoryId = catCivil.Id, MunicipalityId = null,
                WebsiteUrl = "https://uslugi.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Death Certificate", NameMk = "Извод од матична книга на умрените",
                Description = "Extract from the death register",
                DescriptionMk = "Извод за смрт",
                ServiceCategoryId = catCivil.Id, MunicipalityId = null,
                WebsiteUrl = "https://uslugi.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Residence Registration (Skopje)", NameMk = "Пријава на живеалиште (Скопје)",
                Description = "Registration or change of residence address in Skopje",
                DescriptionMk = "Пријава или промена на адреса на живеење во Скопје",
                ServiceCategoryId = catCivil.Id, MunicipalityId = skopje.Id,
                WebsiteUrl = "https://skopje.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Residence Registration (Bitola)", NameMk = "Пријава на живеалиште (Битола)",
                Description = "Registration or change of address in Bitola",
                DescriptionMk = "Пријава или промена на адреса во Битола",
                ServiceCategoryId = catCivil.Id, MunicipalityId = bitola.Id,
                WebsiteUrl = "https://bitola.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Residence Registration (Tetovo)", NameMk = "Пријава на живеалиште (Тетово)",
                Description = "Registration or change of address in Tetovo",
                DescriptionMk = "Пријава или промена на адреса во Тетово",
                ServiceCategoryId = catCivil.Id, MunicipalityId = tetovo.Id,
                WebsiteUrl = "https://tetovo.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Vehicle Registration", NameMk = "Регистрација на моторно возило",
                Description = "First registration or renewal of vehicle registration",
                DescriptionMk = "Прва регистрација или продолжување на регистрација",
                ServiceCategoryId = catVehicle.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.uv.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Vehicle Ownership Transfer", NameMk = "Пренос на сопственост на возило",
                Description = "Transfer of ownership between individuals",
                DescriptionMk = "Пренос на сопственост меѓу физички лица",
                ServiceCategoryId = catVehicle.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.uv.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Annual Tax Return (PIT)", NameMk = "Годишна даночна пријава",
                Description = "Annual personal income tax return",
                DescriptionMk = "Годишна пријава за данок на личен доход",
                ServiceCategoryId = catTax.Id, MunicipalityId = null,
                WebsiteUrl = "https://etax.ujp.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Tax Clearance Certificate", NameMk = "Потврда за платени даноци",
                Description = "Certificate of no tax debt",
                DescriptionMk = "Потврда дека нема долг кон УЈП",
                ServiceCategoryId = catTax.Id, MunicipalityId = null,
                WebsiteUrl = "https://etax.ujp.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Property Tax Declaration", NameMk = "Пријава за данок на имот",
                Description = "Annual property tax declaration",
                DescriptionMk = "Годишна пријава за данок на имот",
                ServiceCategoryId = catTax.Id, MunicipalityId = null,
                WebsiteUrl = "https://etax.ujp.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Guaranteed Minimum Assistance", NameMk = "Гарантирана минимална помош",
                Description = "Social cash assistance for low-income persons",
                DescriptionMk = "Социјална парична помош за лица со ниски приходи",
                ServiceCategoryId = catSocial.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.mtsp.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Child Allowance", NameMk = "Детски додаток",
                Description = "Monthly child allowance",
                DescriptionMk = "Месечен додаток за деца",
                ServiceCategoryId = catSocial.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.mtsp.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Health Insurance Card", NameMk = "Здравствена картичка",
                Description = "Issuing or replacement of health insurance card",
                DescriptionMk = "Издавање или замена на здравствена картичка",
                ServiceCategoryId = catHealth.Id, MunicipalityId = null,
                WebsiteUrl = "https://fzo.org.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Health Insurance Registration", NameMk = "Пријава за здравствено осигурување",
                Description = "Registration as insured person at HIF",
                DescriptionMk = "Пријава како осигуреник во ФЗО",
                ServiceCategoryId = catHealth.Id, MunicipalityId = null,
                WebsiteUrl = "https://fzo.org.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Company Registration", NameMk = "Регистрација на трговско друштво",
                Description = "Founding LLC or JSC via Central Register",
                DescriptionMk = "Основање на ДОО, ДООЕЛ или АД преку Централен регистар",
                ServiceCategoryId = catBusiness.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.crm.com.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "VAT Registration", NameMk = "Регистрација за ДДВ",
                Description = "VAT registration",
                DescriptionMk = "Пријава за обврска за ДДВ",
                ServiceCategoryId = catBusiness.Id, MunicipalityId = null,
                WebsiteUrl = "https://etax.ujp.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Property Certificate (Imoten List)", NameMk = "Имотен лист",
                Description = "Cadastre extract for real estate",
                DescriptionMk = "Извод од катастар за недвижен имот",
                ServiceCategoryId = catProperty.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.katastar.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Cadastral Plan Copy", NameMk = "Препис од катастарски план",
                Description = "Copy of cadastral plan",
                DescriptionMk = "Копија од катастарски план",
                ServiceCategoryId = catProperty.Id, MunicipalityId = null,
                WebsiteUrl = "https://www.katastar.gov.mk", PhoneNumber = null, IsActive = true
            },
            new() {
                Name = "Urban Planning Consent (Skopje)", NameMk = "Урбанистичка согласност (Скопје)",
                Description = "Building consent for the City of Skopje area",
                DescriptionMk = "Согласност за градба на подрачјето на Град Скопје",
                ServiceCategoryId = catProperty.Id, MunicipalityId = skopje.Id,
                WebsiteUrl = "https://skopje.gov.mk", PhoneNumber = null, IsActive = true
            },
        };

        db.CitizenServices.AddRange(services);
        await db.SaveChangesAsync();
    }
}