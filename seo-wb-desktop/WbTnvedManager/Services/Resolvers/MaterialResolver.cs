using System;
using System.Collections.Generic;
using System.Linq;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services.Resolvers
{
    public enum FiberClass
    {
        COTTON,
        SYNTHETIC,
        ARTIFICIAL,
        CHEMICAL_UNSPECIFIED,
        WOOL,
        CASHMERE,
        SILK,
        FLAX,
        RAMIE,
        JUTE,
        UNRESOLVED_MARKETING_TERM,
        OTHER
    }

    public class MaterialResolutionResult
    {
        public bool IsValid { get; set; } = true;
        public string MaterialClass { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string? ReasonCode { get; set; }
        public string ExplanationVi { get; set; } = string.Empty;
        public decimal TotalPercentage { get; set; }
        public decimal CottonPercentage { get; set; }
        public decimal SyntheticPercentage { get; set; }
        public decimal ArtificialPercentage { get; set; }
        public decimal WoolPercentage { get; set; }
        public decimal SilkPercentage { get; set; }
        public decimal FlaxPercentage { get; set; }
    }

    public static class MaterialResolver
    {
        public static FiberClass ClassifyFiber(string fiberName)
        {
            if (string.IsNullOrWhiteSpace(fiberName)) return FiberClass.OTHER;
            var name = fiberName.Trim().ToLowerInvariant();

            if (name.Contains("хлопок") || name.Contains("cotton") || name.Contains("bông") || name.Contains("coton"))
                return FiberClass.COTTON;

            if (name.Contains("полиэстер") || name.Contains("polyester") || name.Contains("полиамид") || 
                name.Contains("polyamide") || name.Contains("nylon") || name.Contains("нейлон") || 
                name.Contains("акрил") || name.Contains("acrylic") || name.Contains("эластан") || 
                name.Contains("elastane") || name.Contains("спандекс") || name.Contains("spandex") ||
                name.Contains("синтетик") || name.Contains("synthetic"))
                return FiberClass.SYNTHETIC;

            if (name.Contains("вискоза") || name.Contains("viscose") || name.Contains("модал") || 
                name.Contains("modal") || name.Contains("лиоцелл") || name.Contains("lyocell") || 
                name.Contains("ацетат") || name.Contains("acetate") || name.Contains("искусствен") || 
                name.Contains("artificial"))
                return FiberClass.ARTIFICIAL;

            if (name.Contains("химические") || name.Contains("chemical"))
                return FiberClass.CHEMICAL_UNSPECIFIED;

            if (name.Contains("кашемир") || name.Contains("cashmere"))
                return FiberClass.CASHMERE;

            if (name.Contains("шерсть") || name.Contains("wool") || name.Contains("len"))
                return FiberClass.WOOL;

            if (name.Contains("шелк") || name.Contains("шёлк") || name.Contains("silk") || name.Contains("tơ tằm"))
                return FiberClass.SILK;

            if (name.Contains("лен") || name.Contains("лён") || name.Contains("flax") || name.Contains("linen") || name.Contains("lanh"))
                return FiberClass.FLAX;

            if (name.Contains("рами") || name.Contains("ramie"))
                return FiberClass.RAMIE;

            if (name.Contains("джут") || name.Contains("jute"))
                return FiberClass.JUTE;

            if (name.Contains("бамбук") || name.Contains("bamboo") || name.Contains("экокожа") || name.Contains("microfibre"))
                return FiberClass.UNRESOLVED_MARKETING_TERM;

            return FiberClass.OTHER;
        }

        public static MaterialResolutionResult Resolve(List<CompositionComponent> components, string determiningPart = "SHELL")
        {
            var res = new MaterialResolutionResult();

            if (components == null || components.Count == 0)
            {
                res.IsValid = false;
                res.ReasonCode = "NEEDS_DATA";
                res.ExplanationVi = "Chưa có thông tin thành phần vải.";
                return res;
            }

            var activeComponents = components.Where(c => string.IsNullOrEmpty(c.ComponentPart) || 
                                                        c.ComponentPart.Equals(determiningPart, StringComparison.OrdinalIgnoreCase)).ToList();

            if (activeComponents.Count == 0)
            {
                activeComponents = components;
            }

            decimal total = activeComponents.Sum(c => c.Percentage);
            res.TotalPercentage = total;

            if (total != 100m)
            {
                res.IsValid = false;
                res.ReasonCode = "INVALID_COMPOSITION_TOTAL";
                res.ExplanationVi = $"Tổng tỷ lệ thành phần vải bằng {total}% (yêu cầu đúng 100%).";
                return res;
            }

            decimal cotton = 0, synthetic = 0, artificial = 0, wool = 0, silk = 0, flax = 0, ramie = 0, cashmere = 0, jute = 0, chemicalUnspecified = 0, other = 0;

            foreach (var comp in activeComponents)
            {
                var fClass = ClassifyFiber(comp.FiberName);
                switch (fClass)
                {
                    case FiberClass.COTTON: cotton += comp.Percentage; break;
                    case FiberClass.SYNTHETIC: synthetic += comp.Percentage; break;
                    case FiberClass.ARTIFICIAL: artificial += comp.Percentage; break;
                    case FiberClass.CHEMICAL_UNSPECIFIED: chemicalUnspecified += comp.Percentage; break;
                    case FiberClass.WOOL: wool += comp.Percentage; break;
                    case FiberClass.CASHMERE: cashmere += comp.Percentage; break;
                    case FiberClass.SILK: silk += comp.Percentage; break;
                    case FiberClass.FLAX: flax += comp.Percentage; break;
                    case FiberClass.RAMIE: ramie += comp.Percentage; break;
                    case FiberClass.JUTE: jute += comp.Percentage; break;
                    case FiberClass.UNRESOLVED_MARKETING_TERM:
                    case FiberClass.OTHER:
                    default:
                        other += comp.Percentage;
                        break;
                }
            }

            res.CottonPercentage = cotton;
            res.SyntheticPercentage = synthetic;
            res.ArtificialPercentage = artificial;
            res.WoolPercentage = wool;
            res.SilkPercentage = silk;
            res.FlaxPercentage = flax;

            // 1. Pure classes (100%)
            if (cotton == 100m) { res.MaterialClass = "COTTON"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Cotton"; return res; }
            if (synthetic == 100m) { res.MaterialClass = "SYNTHETIC"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Sợi tổng hợp"; return res; }
            if (artificial == 100m) { res.MaterialClass = "ARTIFICIAL"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Sợi nhân tạo (viscose/modal)"; return res; }
            if (wool == 100m) { res.MaterialClass = "WOOL"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Len"; return res; }
            if (cashmere == 100m) { res.MaterialClass = "CASHMERE"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Cashmere"; return res; }
            if (silk == 100m) { res.MaterialClass = "SILK"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Tơ tằm"; return res; }
            if (flax == 100m) { res.MaterialClass = "FLAX"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Lanh"; return res; }
            if (ramie == 100m) { res.MaterialClass = "RAMIE"; res.Method = "PURE_CLASS"; res.ExplanationVi = "100% Ramie"; return res; }

            // 2. Cotton + Synthetic only resolver (groups all synthetic fibers)
            if (cotton > 0 && synthetic > 0 && (cotton + synthetic == 100m))
            {
                res.Method = "COTTON_SYNTHETIC_ONLY";
                if (synthetic >= cotton)
                {
                    res.MaterialClass = "SYNTHETIC";
                    res.ExplanationVi = $"Hỗn hợp cotton ({cotton}%) và sợi tổng hợp ({synthetic}%): Sợi tổng hợp chiếm ưu thế ({synthetic}% >= {cotton}%).";
                }
                else
                {
                    res.MaterialClass = "COTTON";
                    res.ExplanationVi = $"Hỗn hợp cotton ({cotton}%) và sợi tổng hợp ({synthetic}%): Cotton chiếm ưu thế ({cotton}% > {synthetic}%).";
                }
                return res;
            }

            // Chemical unspecified without full convergence is not PURE_CLASS in MVP
            if (chemicalUnspecified == 100m)
            {
                res.IsValid = false;
                res.ReasonCode = "MATERIAL_RESOLVER_NOT_IMPLEMENTED";
                res.ExplanationVi = "Khai báo 100% sợi hóa học chung chung; MVP chưa có resolver hội tụ để chọn giữa tổng hợp và nhân tạo.";
                return res;
            }

            // Other mixtures: Resolver not implemented yet in MVP
            res.IsValid = false;
            res.ReasonCode = "MATERIAL_RESOLVER_NOT_IMPLEMENTED";
            res.ExplanationVi = $"Hỗn hợp phức hợp chưa có resolver trong MVP (Cotton: {cotton}%, Synthetic: {synthetic}%, Artificial: {artificial}%, Wool: {wool}%, Flax: {flax}%, Jute: {jute}%).";
            return res;
        }
    }
}
