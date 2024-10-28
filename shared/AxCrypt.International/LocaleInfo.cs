using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AxCrypt.International
{
    /// <summary>
    /// Carrier of language and region information in order to bridge the problem of not
    /// being able to register custom cultures in process.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class LocaleInfo : IEquatable<LocaleInfo>
    {
        public static readonly LocaleInfo Empty = new LocaleInfo();

        public static readonly LocaleInfo SE;

        public static readonly LocaleInfo US;

        static LocaleInfo()
        {
            foreach (string[] pair in DefaultCountryCultureMapping)
            {
                _defaultCountryCultureDictionary.Add(pair[0], pair[1]);
            }
            foreach (string[] pair in DefaultLanguageCountryMapping)
            {
                _defaultLanguageCountryDictionary.Add(pair[0], pair[1]);
            }

            SE = Create("SE");
            US = Create("US");
        }

        public CultureInfo LanguageCulture { get { return CultureInfo.GetCultureInfo(LanguageCultureName); } }

        /// <summary>
        /// Gets the name of the country as a 2 letter ISO code.
        /// </summary>
        /// <value>
        /// The name of the country.
        /// </value>
        public string CountryName
        {
            get
            {
                if (string.IsNullOrEmpty(CultureName))
                {
                    return string.Empty;
                }
                return CultureName.Substring(CultureName.Length - 2, 2);
            }
        }

        [JsonProperty("language_culture_name")]
        private string LanguageCultureName { get; }

        [JsonProperty("region_native_name")]
        public string RegionNativeName { get; }

        [JsonProperty("culture_name")]
        public string CultureName { get; }

        public char Delimiter
        {
            get
            {
                if (LanguageCulture.NumberFormat.CurrencyDecimalSeparator == ",")
                {
                    return ';';
                }
                return ',';
            }
        }

        public static LocaleInfo Create(string cultureNameOrLanguageOrCountry)
        {
            cultureNameOrLanguageOrCountry = cultureNameOrLanguageOrCountry ?? string.Empty;
            switch (cultureNameOrLanguageOrCountry)
            {
                case "el-CY":
                    return new LocaleInfo("el-CY", "el-GR", "Κύπρος");

                case "tr-CY":
                    return new LocaleInfo("tr-CY", "tr-TR", "Kıbrıs");

                case "en-UK":
                    return new LocaleInfo("en-UK", "en-GB", "United Kingdom");
            }

            string specificCulture = FindSpecificIfAny(cultureNameOrLanguageOrCountry);
            if (specificCulture == null)
            {
                throw new InvalidOperationException("Can't use a null culture.");
            }

            switch (specificCulture)
            {
                case "":
                    return Empty;

                default:
                    return new LocaleInfo(specificCulture, specificCulture, new RegionInfo(specificCulture).NativeName);
            }
        }

        private LocaleInfo()
        {
            CultureName = string.Empty;
            LanguageCultureName = "en-US";
            RegionNativeName = string.Empty;
        }

        [JsonConstructor]
        private LocaleInfo(string cultureName, string languageCultureName, string regionNativeName)
        {
            CultureName = cultureName ?? string.Empty;
            LanguageCultureName = languageCultureName;
            RegionNativeName = regionNativeName ?? string.Empty;
        }

        private static readonly IEnumerable<CultureInfo> _specificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

        private static string FindSpecificIfAny(string cultureNameOrLanguageOrCountry)
        {
            if (cultureNameOrLanguageOrCountry.Length == 0)
            {
                return string.Empty;
            }

            if (cultureNameOrLanguageOrCountry.Contains("-"))
            {
                return _specificCultures.Where(c => c.Name == cultureNameOrLanguageOrCountry).Select(c => c.Name).FirstOrDefault() ?? string.Empty;
            }

            string country;
            if (_defaultLanguageCountryDictionary.TryGetValue(cultureNameOrLanguageOrCountry, out country))
            {
                cultureNameOrLanguageOrCountry = country;
            }

            string specific = string.Empty;
            if (_defaultCountryCultureDictionary.TryGetValue(cultureNameOrLanguageOrCountry, out specific) && _specificCultures.Any(c => c.Name == specific))
            {
                return specific;
            }

            string countrySuffix = "-" + cultureNameOrLanguageOrCountry.Split('-').Last();
            IEnumerable<string> candidates = _specificCultures.Where(c => c.Name.EndsWith(countrySuffix)).Select(c => c.Name);

            return candidates.FirstOrDefault() ?? string.Empty;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LocaleInfo);
        }

        public override int GetHashCode()
        {
            return LanguageCultureName.GetHashCode() ^ RegionNativeName.GetHashCode() ^ CultureName.GetHashCode();
        }

        public bool Equals(LocaleInfo other)
        {
            if (ReferenceEquals(other, null) || GetType() != other.GetType())
            {
                return false;
            }

            return LanguageCultureName == other.LanguageCultureName && RegionNativeName == other.RegionNativeName && CultureName == other.CultureName;
        }

        public static bool operator ==(LocaleInfo left, LocaleInfo right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(LocaleInfo left, LocaleInfo right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return CultureName;
        }

        private static Dictionary<string, string> _defaultLanguageCountryDictionary = new Dictionary<string, string>();

        public static readonly string[][] DefaultLanguageCountryMapping = new string[][]
        {
            new string[] { "am", "ET", "Amharic", "Ethiopia" },
            new string[] { "ar", "SA", "Arabic", "Saudi Arabia" },
            new string[] { "az", "AZ", "Unknown", "Unknown Region (AZ)" },
            new string[] { "be", "BY", "Belarusian", "Belarus" },
            new string[] { "bg", "BG", "Bulgarian", "Bulgaria" },
            new string[] { "bi", "VU", "Unknown", "Unknown Region (VU)" },
            new string[] { "bn", "BD", "Bangla", "Bangladesh" },
            new string[] { "bs", "BA", "Unknown", "Unknown Region (BA)" },
            new string[] { "ca", "AD", "Catalan", "Andorra" },
            new string[] { "ch", "MP", "Unknown", "Unknown Region (MP)" },
            new string[] { "cs", "CZ", "Czech", "Czech Republic" },
            new string[] { "da", "DK", "Danish", "Denmark" },
            new string[] { "de", "DE", "German", "Germany" },
            new string[] { "dv", "MV", "Divehi", "Maldives" },
            new string[] { "dz", "BT", "Dzongkha", "Bhutan" },
            new string[] { "el", "GR", "Greek", "Greece" },
            new string[] { "en", "US", "English", "United States" },
            new string[] { "es", "ES", "Spanish", "Spain" },
            new string[] { "et", "EE", "Estonian", "Estonia" },
            new string[] { "fa", "IR", "Persian", "Iran" },
            new string[] { "fi", "FI", "Finnish", "Finland" },
            new string[] { "fo", "FO", "Faroese", "Faroe Islands" },
            new string[] { "fr", "FR", "French", "France" },
            new string[] { "he", "IL", "Hebrew", "Israel" },
            new string[] { "hi", "IN", "Hindi", "India" },
            new string[] { "hr", "HR", "Croatian", "Croatia" },
            new string[] { "hu", "HU", "Hungarian", "Hungary" },
            new string[] { "hy", "AM", "Armenian", "Armenia" },
            new string[] { "id", "ID", "Indonesian", "Indonesia" },
            new string[] { "is", "IS", "Icelandic", "Iceland" },
            new string[] { "it", "IT", "Italian", "Italy" },
            new string[] { "ja", "JP", "Japanese", "Japan" },
            new string[] { "ka", "GE", "Georgian", "Georgia" },
            new string[] { "kk", "KZ", "Kazakh", "Kazakhstan" },
            new string[] { "kl", "GL", "Greenlandic", "Greenland" },
            new string[] { "km", "KH", "Khmer", "Cambodia" },
            new string[] { "ko", "KR", "Korean", "Korea" },
            new string[] { "ky", "KG", "Kyrgyz", "Kyrgyzstan" },
            new string[] { "lb", "LU", "Luxembourgish", "Luxembourg" },
            new string[] { "lo", "LA", "Lao", "Laos" },
            new string[] { "lt", "LT", "Lithuanian", "Lithuania" },
            new string[] { "lv", "LV", "Latvian", "Latvia" },
            new string[] { "mg", "MG", "Malagasy", "Madagascar" },
            new string[] { "mk", "MK", "Macedonian", "Macedonia, FYRO" },
            new string[] { "mn", "MN", "Mongolian", "Mongolia" },
            new string[] { "ms", "MY", "Malay", "Malaysia" },
            new string[] { "mt", "MT", "Maltese", "Malta" },
            new string[] { "my", "MM", "Burmese", "Myanmar" },
            new string[] { "na", "NR", "Unknown", "Unknown Region (NR)" },
            new string[] { "nb", "NO", "Norwegian", "Norway" },
            new string[] { "ne", "NP", "Nepali", "Nepal" },
            new string[] { "ni", "NU", "Unknown", "Unknown Region (NU)" },
            new string[] { "nl", "NL", "Dutch", "Netherlands" },
            new string[] { "pl", "PL", "Polish", "Poland" },
            new string[] { "pt", "PT", "Portuguese", "Portugal" },
            new string[] { "ro", "RO", "Romanian", "Romania" },
            new string[] { "ru", "RU", "Russian", "Russia" },
            new string[] { "rw", "RW", "Kinyarwanda", "Rwanda" },
            new string[] { "si", "LK", "Sinhala", "Sri Lanka" },
            new string[] { "sk", "SK", "Slovak", "Slovakia" },
            new string[] { "sl", "SI", "Slovenian", "Slovenia" },
            new string[] { "sm", "WS", "Unknown", "Unknown Region (WS)" },
            new string[] { "so", "SO", "Somali", "Somalia" },
            new string[] { "sq", "AL", "Albanian", "Albania" },
            new string[] { "sr", "RS", "Serbian", "Serbia"},
            new string[] { "sv", "SE", "Swedish", "Sweden" },
            new string[] { "sw", "KE", "Kiswahili", "Kenya" },
            new string[] { "tg", "TJ", "Tajik", "Tajikistan" },
            new string[] { "th", "TH", "Thai", "Thailand" },
            new string[] { "ti", "ER", "Tigrinya", "Eritrea" },
            new string[] { "tr", "TR", "Turkish", "Turkey" },
            new string[] { "uk", "UA", "Ukrainian", "Ukraine" },
            new string[] { "uz", "UZ", "Unknown", "Unknown Region (UZ)" },
            new string[] { "vi", "VN", "Vietnamese", "Vietnam" },
            new string[] { "zh", "CN", "Chinese", "China" },
            new string[] { "zu", "ZA", "isiZulu", "South Africa" },
        };

        private static Dictionary<string, string> _defaultCountryCultureDictionary = new Dictionary<string, string>();

        /// <summary>
        /// The default country culture mapping - source: http://wiki.openstreetmap.org/wiki/Nominatim/Country_Codes .
        /// </summary>
        public static readonly string[][] DefaultCountryCultureMapping = new string[][]
        {
            // Country, Culture, Language English, Language Nataive, Country English, Country Native
            new string[] { "AD", "ca-AD", "Catalan", "català", "Andorra", "Andorra" },
            new string[] { "AE", "ar-AE", "Arabic", "العربية", "United Arab Emirates", "الإمارات العربية المتحدة" },
            new string[] { "AF", "fa-AF", "Dari", "درى", "Afghanistan", "افغانستان" },
            new string[] { "AG", "en-AG", "English", "English", "Antigua and Barbuda", "Antigua and Barbuda" },
            new string[] { "AI", "en-AI", "English", "English", "Anguilla", "Anguilla" },
            new string[] { "AL", "sq-AL", "Albanian", "shqip", "Albania", "Shqipëri" },
            new string[] { "AM", "hy-AM", "Armenian", "Հայերեն", "Armenia", "Հայաստան" },
            new string[] { "AO", "pt-AO", "Portuguese", "português", "Angola", "Angola" },
            new string[] { "AQ", "en-AQ", "Unknown", "Unknown", "Unknown Region (AQ)", "Unknown Region (AQ)" },
            new string[] { "AR", "es-AR", "Spanish", "español", "Argentina", "Argentina" },
            new string[] { "AS", "en-AS", "English", "English", "American Samoa", "American Samoa" },
            new string[] { "AT", "de-AT", "German", "Deutsch", "Austria", "Österreich" },
            new string[] { "AU", "en-AU", "English", "English", "Australia", "Australia" },
            new string[] { "AW", "nl-AW", "Dutch", "Nederlands", "Aruba", "Aruba" },
            new string[] { "AX", "sv-AX", "Swedish", "svenska", "Åland Islands", "Åland" },
            new string[] { "AZ", "az-AZ", "Unknown", "Unknown", "Unknown Region (AZ)", "Unknown Region (AZ)" },
            new string[] { "BA", "bs-BA", "Unknown", "Unknown", "Unknown Region (BA)", "Unknown Region (BA)" },
            new string[] { "BB", "en-BB", "English", "English", "Barbados", "Barbados" },
            new string[] { "BD", "bn-BD", "Bangla", "বাংলা", "Bangladesh", "বাংলাদেশ" },
            new string[] { "BE", "nl-BE", "Dutch", "Nederlands", "Belgium", "België" },
            new string[] { "BF", "fr-BF", "French", "français", "Burkina Faso", "Burkina Faso" },
            new string[] { "BG", "bg-BG", "Bulgarian", "български", "Bulgaria", "България" },
            new string[] { "BH", "ar-BH", "Arabic", "العربية", "Bahrain", "البحرين" },
            new string[] { "BI", "fr-BI", "French", "français", "Burundi", "Burundi" },
            new string[] { "BJ", "fr-BJ", "French", "français", "Benin", "Bénin" },
            new string[] { "BL", "fr-BL", "French", "français", "Saint Barthélemy", "Saint-Barthélemy" },
            new string[] { "BM", "en-BM", "English", "English", "Bermuda", "Bermuda" },
            new string[] { "BN", "ms-BN", "Malay", "Bahasa", "Brunei", "Brunei" },
            new string[] { "BO", "es-BO", "Spanish", "español", "Bolivia", "Bolivia" },
            new string[] { "BQ", "nl-BQ", "Dutch", "Nederlands", "Bonaire, Sint Eustatius and Saba", "Bonaire, Sint Eustatius en Saba" },
            new string[] { "BR", "pt-BR", "Portuguese", "português", "Brazil", "Brasil" },
            new string[] { "BS", "en-BS", "English", "English", "Bahamas", "Bahamas" },
            new string[] { "BT", "dz-BT", "Dzongkha", "རྫོང་ཁ", "Bhutan", "འབྲུག" },
            new string[] { "BV", "no-BV", "Unknown", "Unknown", "Unknown Region (BV)", "Unknown Region (BV)" },
            new string[] { "BW", "en-BW", "English", "English", "Botswana", "Botswana" },
            new string[] { "BY", "be-BY", "Belarusian", "Беларуская", "Belarus", "Беларусь" },
            new string[] { "BZ", "en-BZ", "English", "English", "Belize", "Belize" },
            new string[] { "CA", "en-CA", "English", "English", "Canada", "Canada" },
            new string[] { "CC", "en-CC", "English", "English", "Cocos (Keeling) Islands", "Cocos (Keeling) Islands" },
            new string[] { "CD", "fr-CD", "French", "français", "Congo (DRC)", "Congo, République démocratique du" },
            new string[] { "CF", "fr-CF", "French", "français", "Central African Republic", "République centrafricaine" },
            new string[] { "CG", "fr-CG", "French", "français", "Congo", "Congo" },
            new string[] { "CH", "de-CH", "German", "Deutsch", "Switzerland", "Schweiz" },
            new string[] { "CI", "fr-CI", "French", "français", "Côte d’Ivoire", "Côte d’Ivoire" },
            new string[] { "CK", "en-CK", "English", "English", "Cook Islands", "Cook Islands" },
            new string[] { "CL", "es-CL", "Spanish", "español", "Chile", "Chile" },
            new string[] { "CM", "fr-CM", "French", "français", "Cameroon", "Cameroun" },
            new string[] { "CN", "zh-CN", "Chinese", "中文(中国)", "China", "中国" },
            new string[] { "CO", "es-CO", "Spanish", "español", "Colombia", "Colombia" },
            new string[] { "CR", "es-CR", "Spanish", "español", "Costa Rica", "Costa Rica" },
            new string[] { "CU", "es-CU", "Spanish", "español", "Cuba", "Cuba" },
            new string[] { "CV", "pt-CV", "Portuguese", "português", "Cabo Verde", "Cabo Verde" },
            new string[] { "CW", "nl-CW", "Dutch", "Nederlands", "Curaçao", "Curaçao" },
            new string[] { "CX", "en-CX", "English", "English", "Christmas Island", "Christmas Island" },
            new string[] { "CY", "el-CY", "Greek", "Ελληνικά", "Cyprus", "Κύπρος" },
            new string[] { "CZ", "cs-CZ", "Czech", "čeština", "Czech Republic", "Česká republika" },
            new string[] { "DE", "de-DE", "German", "Deutsch", "Germany", "Deutschland" },
            new string[] { "DJ", "fr-DJ", "French", "français", "Djibouti", "Djibouti" },
            new string[] { "DK", "da-DK", "Danish", "dansk", "Denmark", "Danmark" },
            new string[] { "DM", "en-DM", "English", "English", "Dominica", "Dominica" },
            new string[] { "DO", "es-DO", "Spanish", "español", "Dominican Republic", "República Dominicana" },
            new string[] { "DZ", "ar-DZ", "Arabic", "العربية", "Algeria", "الجزائر" },
            new string[] { "EC", "es-EC", "Spanish", "español", "Ecuador", "Ecuador" },
            new string[] { "EE", "et-EE", "Estonian", "eesti", "Estonia", "Eesti" },
            new string[] { "EG", "ar-EG", "Arabic", "العربية", "Egypt", "مصر" },
            new string[] { "EH", "ar-EH", "Unknown", "Unknown", "Unknown Region (EH)", "Unknown Region (EH)" },
            new string[] { "ER", "ti-ER", "Tigrinya", "ትግርኛ", "Eritrea", "ኤርትራ" },
            new string[] { "ES", "es-ES", "Spanish", "español", "Spain", "España" },
            new string[] { "ET", "am-ET", "Amharic", "አማርኛ", "Ethiopia", "ኢትዮጵያ" },
            new string[] { "FI", "fi-FI", "Finnish", "suomi", "Finland", "Suomi" },
            new string[] { "FJ", "en-FJ", "English", "English", "Fiji", "Fiji" },
            new string[] { "FK", "en-FK", "English", "English", "Falkland Islands", "Falkland Islands" },
            new string[] { "FM", "en-FM", "English", "English", "Micronesia", "Micronesia" },
            new string[] { "FO", "fo-FO", "Faroese", "føroyskt", "Faroe Islands", "Føroyar" },
            new string[] { "FR", "fr-FR", "French", "français", "France", "France" },
            new string[] { "GA", "fr-GA", "French", "français", "Gabon", "Gabon" },
            new string[] { "GB", "en-GB", "English", "English", "United Kingdom", "United Kingdom" },
            new string[] { "GD", "en-GD", "English", "English", "Grenada", "Grenada" },
            new string[] { "GE", "ka-GE", "Georgian", "ქართული", "Georgia", "საქართველო" },
            new string[] { "GF", "fr-GF", "French", "français", "French Guiana", "Guyane française" },
            new string[] { "GG", "en-GG", "English", "English", "Guernsey", "Guernsey" },
            new string[] { "GH", "en-GH", "English", "English", "Ghana", "Ghana" },
            new string[] { "GI", "en-GI", "English", "English", "Gibraltar", "Gibraltar" },
            new string[] { "GL", "kl-GL", "Greenlandic", "kalaallisut", "Greenland", "Kalaallit Nunaat" },
            new string[] { "GM", "en-GM", "English", "English", "Gambia", "Gambia" },
            new string[] { "GN", "fr-GN", "French", "français", "Guinea", "Guinée" },
            new string[] { "GP", "fr-GP", "French", "français", "Guadeloupe", "Guadeloupe" },
            new string[] { "GQ", "es-GQ", "Spanish", "español", "Equatorial Guinea", "Guinea Ecuatorial" },
            new string[] { "GR", "el-GR", "Greek", "Ελληνικά", "Greece", "Ελλάδα" },
            new string[] { "GS", "en-GS", "Unknown", "Unknown", "Unknown Region (GS)", "Unknown Region (GS)" },
            new string[] { "GT", "es-GT", "Spanish", "español", "Guatemala", "Guatemala" },
            new string[] { "GU", "en-GU", "English", "English", "Guam", "Guam" },
            new string[] { "GW", "pt-GW", "Portuguese", "português", "Guinea-Bissau", "Guiné-Bissau" },
            new string[] { "GY", "en-GY", "English", "English", "Guyana", "Guyana" },
            new string[] { "HK", "zh-HK", "Chinese", "中文(香港特別行政區)", "Hong Kong SAR", "香港特別行政區" },
            new string[] { "HM", "en-HM", "Unknown", "Unknown", "Unknown Region (HM)", "Unknown Region (HM)" },
            new string[] { "HN", "es-HN", "Spanish", "español", "Honduras", "Honduras" },
            new string[] { "HR", "hr-HR", "Croatian", "hrvatski", "Croatia", "Hrvatska" },
            new string[] { "HT", "fr-HT", "French", "français", "Haiti", "Haïti" },
            new string[] { "HU", "hu-HU", "Hungarian", "magyar", "Hungary", "Magyarország" },
            new string[] { "ID", "id-ID", "Indonesian", "Indonesia", "Indonesia", "Indonesia" },
            new string[] { "IE", "en-IE", "English", "English", "Ireland", "Ireland" },
            new string[] { "IL", "he-IL", "Hebrew", "עברית", "Israel", "ישראל" },
            new string[] { "IM", "en-IM", "English", "English", "Isle of Man", "Isle of Man" },
            new string[] { "IN", "hi-IN", "Hindi", "हिंदी", "India", "भारत" },
            new string[] { "IO", "en-IO", "English", "English", "British Indian Ocean Territory", "British Indian Ocean Territory" },
            new string[] { "IQ", "ar-IQ", "Arabic", "العربية", "Iraq", "العراق" },
            new string[] { "IR", "fa-IR", "Persian", "فارسى", "Iran", "ایران" },
            new string[] { "IS", "is-IS", "Icelandic", "íslenska", "Iceland", "Ísland" },
            new string[] { "IT", "it-IT", "Italian", "italiano", "Italy", "Italia" },
            new string[] { "JE", "en-JE", "English", "English", "Jersey", "Jersey" },
            new string[] { "JM", "en-JM", "English", "English", "Jamaica", "Jamaica" },
            new string[] { "JO", "ar-JO", "Arabic", "العربية", "Jordan", "الأردن" },
            new string[] { "JP", "ja-JP", "Japanese", "日本語", "Japan", "日本" },
            new string[] { "KE", "sw-KE", "Kiswahili", "Kiswahili", "Kenya", "Kenya" },
            new string[] { "KG", "ky-KG", "Kyrgyz", "Кыргыз", "Kyrgyzstan", "Кыргызстан" },
            new string[] { "KH", "km-KH", "Khmer", "ភាសាខ្មែរ", "Cambodia", "កម្ពុជា" },
            new string[] { "KI", "en-KI", "English", "English", "Kiribati", "Kiribati" },
            new string[] { "KM", "ar-KM", "Arabic", "العربية", "Comoros", "جزر القمر" },
            new string[] { "KN", "en-KN", "English", "English", "Saint Kitts and Nevis", "Saint Kitts and Nevis" },
            new string[] { "KP", "ko-KP", "Korean", "한국어", "North Korea", "조선민주주의인민공화국" },
            new string[] { "KR", "ko-KR", "Korean", "한국어(대한민국)", "Korea", "대한민국" },
            new string[] { "KW", "ar-KW", "Arabic", "العربية", "Kuwait", "الكويت" },
            new string[] { "KY", "en-KY", "English", "English", "Cayman Islands", "Cayman Islands" },
            new string[] { "KZ", "kk-KZ", "Kazakh", "қазақ", "Kazakhstan", "Қазақстан" },
            new string[] { "LA", "lo-LA", "Lao", "ລາວ", "Laos", "ລາວ" },
            new string[] { "LB", "ar-LB", "Arabic", "العربية", "Lebanon", "لبنان" },
            new string[] { "LC", "en-LC", "English", "English", "Saint Lucia", "Saint Lucia" },
            new string[] { "LI", "de-LI", "German", "Deutsch", "Liechtenstein", "Liechtenstein" },
            new string[] { "LK", "si-LK", "Sinhala", "සිංහල", "Sri Lanka", "ශ්‍රී ලංකාව" },
            new string[] { "LR", "en-LR", "English", "English", "Liberia", "Liberia" },
            new string[] { "LS", "en-LS", "English", "English", "Lesotho", "Lesotho" },
            new string[] { "LT", "lt-LT", "Lithuanian", "lietuvių", "Lithuania", "Lietuva" },
            new string[] { "LU", "lb-LU", "Luxembourgish", "Lëtzebuergesch", "Luxembourg", "Lëtzebuerg" },
            new string[] { "LV", "lv-LV", "Latvian", "latviešu", "Latvia", "Latvija" },
            new string[] { "LY", "ar-LY", "Arabic", "العربية", "Libya", "ليبيا" },
            new string[] { "MA", "ar-MA", "Arabic", "العربية", "Morocco", "المملكة المغربية" },
            new string[] { "MC", "fr-MC", "French", "français", "Monaco", "Monaco" },
            new string[] { "MD", "ru-MD", "Russian", "русский", "Moldova", "Молдова" },
            new string[] { "ME", "srp-ME", "Unknown", "Unknown", "Unknown Region (ME)", "Unknown Region (ME)" },
            new string[] { "MF", "fr-MF", "French", "français", "Saint Martin", "Saint-Martin" },
            new string[] { "MG", "mg-MG", "Malagasy", "Malagasy", "Madagascar", "Madagasikara" },
            new string[] { "MH", "en-MH", "English", "English", "Marshall Islands", "Marshall Islands" },
            new string[] { "MK", "mk-MK", "Macedonian", "македонски", "Macedonia, FYRO", "Република Македонија" },
            new string[] { "ML", "fr-ML", "French", "français", "Mali", "Mali" },
            new string[] { "MM", "my-MM", "Burmese", "ဗမာ", "Myanmar", "မြန်မာ" },
            new string[] { "MN", "mn-MN", "Mongolian", "монгол", "Mongolia", "Монгол" },
            new string[] { "MO", "zh-MO", "Chinese", "中文(澳門特別行政區)", "Macao SAR", "澳門特別行政區" },
            new string[] { "MP", "ch-MP", "Unknown", "Unknown", "Unknown Region (MP)", "Unknown Region (MP)" },
            new string[] { "MQ", "fr-MQ", "French", "français", "Martinique", "Martinique" },
            new string[] { "MR", "ar-MR", "Arabic", "العربية", "Mauritania", "موريتانيا" },
            new string[] { "MS", "en-MS", "English", "English", "Montserrat", "Montserrat" },
            new string[] { "MT", "mt-MT", "Maltese", "Malti", "Malta", "Malta" },
            new string[] { "MU", "fr-MU", "French", "français", "Mauritius", "Maurice" },
            new string[] { "MV", "dv-MV", "Divehi", "ދިވެހިބަސް", "Maldives", "ދިވެހި ރާއްޖެ" },
            new string[] { "MW", "en-MW", "English", "English", "Malawi", "Malawi" },
            new string[] { "MX", "es-MX", "Spanish", "español", "Mexico", "México" },
            new string[] { "MY", "ms-MY", "Malay", "Bahasa", "Malaysia", "Malaysia" },
            new string[] { "MZ", "pt-MZ", "Portuguese", "português", "Mozambique", "Moçambique" },
            new string[] { "NA", "en-NA", "English", "English", "Namibia", "Namibia" },
            new string[] { "NC", "fr-NC", "French", "français", "New Caledonia", "Nouvelle-Calédonie" },
            new string[] { "NE", "fr-NE", "French", "français", "Niger", "Niger" },
            new string[] { "NF", "en-NF", "English", "English", "Norfolk Island", "Norfolk Island" },
            new string[] { "NG", "en-NG", "English", "English", "Nigeria", "Nigeria" },
            new string[] { "NI", "es-NI", "Spanish", "español", "Nicaragua", "Nicaragua" },
            new string[] { "NL", "nl-NL", "Dutch", "Nederlands", "Netherlands", "Nederland" },
            new string[] { "NO", "nb-NO", "Norwegian", "norsk", "Norway", "Norge" },
            new string[] { "NP", "ne-NP", "Nepali", "नेपाली", "Nepal", "नेपाल" },
            new string[] { "NR", "na-NR", "Unknown", "Unknown", "Unknown Region (NR)", "Unknown Region (NR)" },
            new string[] { "NU", "niu-NU", "Unknown", "Unknown", "Unknown Region (NU)", "Unknown Region (NU)" },
            new string[] { "NZ", "en-NZ", "English", "English", "New Zealand", "New Zealand" },
            new string[] { "OM", "ar-OM", "Arabic", "العربية", "Oman", "عمان" },
            new string[] { "PA", "es-PA", "Spanish", "español", "Panama", "Panamá" },
            new string[] { "PE", "es-PE", "Spanish", "español", "Peru", "Perú" },
            new string[] { "PF", "fr-PF", "French", "français", "French Polynesia", "Polynésie française" },
            new string[] { "PG", "en-PG", "English", "English", "Papua New Guinea", "Papua New Guinea" },
            new string[] { "PH", "en-PH", "English", "English", "Philippines", "Philippines" },
            new string[] { "PK", "en-PK", "English", "English", "Pakistan", "Pakistan" },
            new string[] { "PL", "pl-PL", "Polish", "polski", "Poland", "Polska" },
            new string[] { "PM", "fr-PM", "French", "français", "Saint Pierre and Miquelon", "Saint-Pierre-et-Miquelon" },
            new string[] { "PN", "en-PN", "English", "English", "Pitcairn Islands", "Pitcairn Islands" },
            new string[] { "PR", "es-PR", "Spanish", "español", "Puerto Rico", "Puerto Rico" },
            new string[] { "PS", "ar-PS", "Arabic", "العربية", "Palestinian Authority", "السلطة الفلسطينية" },
            new string[] { "PT", "pt-PT", "Portuguese", "português", "Portugal", "Portugal" },
            new string[] { "PW", "en-PW", "English", "English", "Palau", "Palau" },
            new string[] { "PY", "es-PY", "Spanish", "español", "Paraguay", "Paraguay" },
            new string[] { "QA", "ar-QA", "Arabic", "العربية", "Qatar", "قطر" },
            new string[] { "RE", "fr-RE", "French", "français", "Réunion", "La Réunion" },
            new string[] { "RO", "ro-RO", "Romanian", "română", "Romania", "România" },
            new string[] { "RS", "sr-RS", "Serbian", "Српски", "Serbia", "Србија" },
            new string[] { "RU", "ru-RU", "Russian", "русский", "Russia", "Россия" },
            new string[] { "RW", "rw-RW", "Kinyarwanda", "Kinyarwanda", "Rwanda", "Rwanda" },
            new string[] { "SA", "ar-SA", "Arabic", "العربية", "Saudi Arabia", "المملكة العربية السعودية" },
            new string[] { "SB", "en-SB", "English", "English", "Solomon Islands", "Solomon Islands" },
            new string[] { "SC", "fr-SC", "French", "français", "Seychelles", "Seychelles" },
            new string[] { "SD", "ar-SD", "Arabic", "العربية", "Sudan", "السودان" },
            new string[] { "SE", "sv-SE", "Swedish", "svenska", "Sweden", "Sverige" },
            new string[] { "SG", "en-SG", "English", "English", "Singapore", "Singapore" },
            new string[] { "SH", "en-SH", "English", "English", "St Helena, Ascension, Tristan da Cunha", "St Helena, Ascension, Tristan da Cunha" },
            new string[] { "SI", "sl-SI", "Slovenian", "slovenščina", "Slovenia", "Slovenija" },
            new string[] { "SJ", "no-SJ", "Unknown", "Unknown", "Unknown Region (SJ)", "Unknown Region (SJ)" },
            new string[] { "SK", "sk-SK", "Slovak", "slovenčina", "Slovakia", "Slovensko" },
            new string[] { "SL", "en-SL", "English", "English", "Sierra Leone", "Sierra Leone" },
            new string[] { "SM", "it-SM", "Italian", "italiano", "San Marino", "San Marino" },
            new string[] { "SN", "fr-SN", "French", "français", "Senegal", "Sénégal" },
            new string[] { "SO", "so-SO", "Somali", "Soomaali", "Somalia", "Soomaaliya" },
            new string[] { "SR", "nl-SR", "Dutch", "Nederlands", "Suriname", "Suriname" },
            new string[] { "ST", "pt-ST", "Portuguese", "português", "São Tomé and Príncipe", "São Tomé e Príncipe" },
            new string[] { "SS", "en-SS", "English", "English", "South Sudan", "South Sudan" },
            new string[] { "SV", "es-SV", "Spanish", "español", "El Salvador", "El Salvador" },
            new string[] { "SX", "nl-SX", "Dutch", "Nederlands", "Sint Maarten", "Sint-Maarten" },
            new string[] { "SY", "ar-SY", "Arabic", "العربية", "Syria", "سوريا" },
            new string[] { "SZ", "en-SZ", "English", "English", "Swaziland", "Swaziland" },
            new string[] { "TC", "en-TC", "English", "English", "Turks and Caicos Islands", "Turks and Caicos Islands" },
            new string[] { "TD", "fr-TD", "French", "français", "Chad", "Tchad" },
            new string[] { "TF", "fr-TF", "Unknown", "Unknown", "Unknown Region (TF)", "Unknown Region (TF)" },
            new string[] { "TG", "fr-TG", "French", "français", "Togo", "Togo" },
            new string[] { "TH", "th-TH", "Thai", "ไทย", "Thailand", "ไทย" },
            new string[] { "TJ", "tg-TJ", "Tajik", "Тоҷикӣ", "Tajikistan", "Тоҷикистон" },
            new string[] { "TK", "tkl-TK", "Unknown", "Unknown", "Unknown Region (TK)", "Unknown Region (TK)" },
            new string[] { "TL", "pt-TL", "Portuguese", "português", "Timor-Leste", "Timor-Leste" },
            new string[] { "TM", "tk-TM", "Turkmen", "Türkmen", "Turkmenistan", "Türkmenistan" },
            new string[] { "TN", "ar-TN", "Arabic", "العربية", "Tunisia", "تونس" },
            new string[] { "TO", "en-TO", "English", "English", "Tonga", "Tonga" },
            new string[] { "TR", "tr-TR", "Turkish", "Türkçe", "Turkey", "Türkiye" },
            new string[] { "TT", "en-TT", "English", "English", "Trinidad and Tobago", "Trinidad and Tobago" },
            new string[] { "TV", "en-TV", "English", "English", "Tuvalu", "Tuvalu" },
            new string[] { "TW", "zh-TW", "Chinese", "中文(台灣)", "Taiwan", "台灣" },
            new string[] { "TZ", "sw-TZ", "Kiswahili", "Kiswahili", "Tanzania", "Tanzania" },
            new string[] { "UA", "uk-UA", "Ukrainian", "українська", "Ukraine", "Україна" },
            new string[] { "UG", "en-UG", "English", "English", "Uganda", "Uganda" },
            new string[] { "UM", "en-UM", "English", "English", "U.S. Outlying Islands", "U.S. Outlying Islands" },
            new string[] { "US", "en-US", "English", "English", "United States", "United States" },
            new string[] { "UY", "es-UY", "Spanish", "español", "Uruguay", "Uruguay" },
            new string[] { "UZ", "uz-UZ", "Unknown", "Unknown", "Unknown Region (UZ)", "Unknown Region (UZ)" },
            new string[] { "VA", "it-VA", "Unknown", "Unknown", "Unknown Region (VA)", "Unknown Region (VA)" },
            new string[] { "VC", "en-VC", "English", "English", "Saint Vincent and the Grenadines", "Saint Vincent and the Grenadines" },
            new string[] { "VE", "es-VE", "Spanish", "español", "Venezuela", "Venezuela" },
            new string[] { "VG", "en-VG", "English", "English", "British Virgin Islands", "British Virgin Islands" },
            new string[] { "VI", "en-VI", "English", "English", "U.S. Virgin Islands", "U.S. Virgin Islands" },
            new string[] { "VN", "vi-VN", "Vietnamese", "Tiếng", "Vietnam", "Việt Nam" },
            new string[] { "VU", "bi-VU", "Unknown", "Unknown", "Unknown Region (VU)", "Unknown Region (VU)" },
            new string[] { "WF", "fr-WF", "French", "français", "Wallis and Futuna", "Wallis-et-Futuna" },
            new string[] { "WS", "sm-WS", "Unknown", "Unknown", "Unknown Region (WS)", "Unknown Region (WS)" },
            new string[] { "YE", "ar-YE", "Arabic", "العربية", "Yemen", "اليمن" },
            new string[] { "YT", "fr-YT", "French", "français", "Mayotte", "Mayotte" },
            new string[] { "ZA", "zu-ZA", "isiZulu", "isiZulu", "South Africa", "i-South Africa" },
            new string[] { "ZM", "en-ZM", "English", "English", "Zambia", "Zambia" },
            new string[] { "ZW", "en-ZW", "English", "English", "Zimbabwe", "Zimbabwe" },
        };
    }
}