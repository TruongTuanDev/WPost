using System;
using System.Collections.Generic;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class CardAuditService
    {
        private readonly TnvedSelectorService _selector;
        private readonly RulesEngine _rulesEngine;

        public CardAuditService(TnvedSelectorService selector)
        {
            _selector = selector;
            _rulesEngine = new RulesEngine(selector);
        }

        public List<AuditResultItem> AuditCards(IEnumerable<WbCardItem> cards)
        {
            var results = new List<AuditResultItem>();

            foreach (var card in cards)
            {
                var currentTnved = card.CurrentTnved?.Trim() ?? string.Empty;
                var currentGender = card.CurrentGender?.Trim() ?? string.Empty;
                var currentMaterial = card.CurrentMaterial?.Trim() ?? string.Empty;

                var textContext = $"{card.SubjectName} {card.Title} {card.Description} {currentMaterial}";
                var detectedGender = _selector.InferGender(textContext, currentGender);
                var detectedMaterial = _selector.InferMaterial(textContext, currentMaterial);
                var detectedKnit = _selector.InferKnitType(textContext);

                var (suggestedTnved, matchReason) = _selector.GetTnvedForAttributes(
                    card.SubjectId, 
                    detectedGender, 
                    detectedMaterial, 
                    detectedKnit,
                    subjectName: card.SubjectName,
                    title: card.Title,
                    fullTextContext: textContext
                );

                var evaluationContext = new AuditEvaluationContext
                {
                    WbCard = card,
                    Account = new SellerAccount()
                };
                var report = _rulesEngine.Evaluate(evaluationContext);

                var item = new AuditResultItem
                {
                    Card = card,
                    CurrentTnved = currentTnved,
                    CurrentGender = currentGender,
                    DetectedMaterial = detectedMaterial,
                    SuggestedTnved = suggestedTnved,
                    SuggestedGender = detectedGender,
                    MatchReason = matchReason,
                    Issues = report.Issues,
                    Readiness = report.Readiness,
                    IsSelected = true
                };

                if (string.IsNullOrEmpty(suggestedTnved))
                {
                    item.Status = AuditStatus.NoMatrixMatch;
                    item.StatusMessage = "Chưa có quy tắc tra cứu trong ma trận cho danh mục này.";
                    item.IsSelected = false;
                }
                else
                {
                    bool tnvedMatches = !string.IsNullOrEmpty(currentTnved) && currentTnved.Equals(suggestedTnved, StringComparison.OrdinalIgnoreCase);
                    bool genderMatches = !string.IsNullOrEmpty(currentGender) && currentGender.Equals(detectedGender, StringComparison.OrdinalIgnoreCase);

                    if (tnvedMatches && genderMatches)
                    {
                        item.Status = AuditStatus.MatchOk;
                        item.StatusMessage = "Mã TNVED và Giới tính đã hoàn toàn chính xác.";
                        item.IsSelected = false; // By default don't re-update OK cards
                    }
                    else if (!tnvedMatches && !genderMatches)
                    {
                        item.Status = AuditStatus.BothMismatch;
                        item.StatusMessage = $"Sai cả TNVED ({currentTnved} -> {suggestedTnved}) và Giới tính ({currentGender} -> {detectedGender})";
                        item.IsSelected = true;
                    }
                    else if (!tnvedMatches)
                    {
                        item.Status = AuditStatus.TnvedMismatch;
                        item.StatusMessage = $"Lệch mã TNVED ({currentTnved} -> {suggestedTnved})";
                        item.IsSelected = true;
                    }
                    else
                    {
                        item.Status = AuditStatus.GenderMismatch;
                        item.StatusMessage = $"Thiếu hoặc sai Giới tính ({currentGender} -> {detectedGender})";
                        item.IsSelected = true;
                    }
                }

                results.Add(item);
            }

            return results;
        }
    }
}
