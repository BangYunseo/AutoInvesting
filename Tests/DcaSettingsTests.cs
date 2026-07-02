using System.Collections.Generic;
using AutoInvest.Core;
using AutoInvest.Data.DTO;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// DcaSettings.SelectTemplate(순수 함수 — 외부 I/O·현재시간 의존 없음)의 단위 검증.
    /// "이번 달에 어떤 매수 템플릿을 적용할지" 선택 규칙이 정확한지 자동으로 확인합니다.
    /// (규칙: 현재 월 템플릿 선택 / 월배정 없을 때 첫 템플릿 / 템플릿 없는 달 스킵)
    /// </summary>
    public class DcaSettingsTests
    {
        private static DcaTemplate Tpl(string id, string name = "") =>
            new DcaTemplate { Id = id, Name = string.IsNullOrEmpty(name) ? id : name };

        /// <summary>이번 달에 배정된 Id의 템플릿을 선택해야 한다.</summary>
        [Fact]
        public void SelectTemplate_배정된달은_해당템플릿을_선택한다()
        {
            var templates = new List<DcaTemplate> { Tpl("aggr", "공격형"), Tpl("safe", "안정형") };
            var monthMap = new Dictionary<int, string> { [3] = "safe" };

            var chosen = DcaSettings.SelectTemplate(templates, monthMap, 3);

            Assert.NotNull(chosen);
            Assert.Equal("safe", chosen!.Id);
        }

        /// <summary>월배정이 비어 있으면 첫(기본) 템플릿을 매월 사용해야 한다.</summary>
        [Fact]
        public void SelectTemplate_월배정이_비면_첫템플릿을_쓴다()
        {
            var templates = new List<DcaTemplate> { Tpl("aggr", "공격형"), Tpl("safe", "안정형") };
            var monthMap = new Dictionary<int, string>(); // 비어 있음

            var chosen = DcaSettings.SelectTemplate(templates, monthMap, 7);

            Assert.NotNull(chosen);
            Assert.Equal("aggr", chosen!.Id); // 첫 템플릿
        }

        /// <summary>월배정은 있으나 이번 달에 배정이 없으면 null(매수 스킵)이어야 한다.</summary>
        [Fact]
        public void SelectTemplate_배정없는달은_스킵한다()
        {
            var templates = new List<DcaTemplate> { Tpl("aggr"), Tpl("safe") };
            var monthMap = new Dictionary<int, string> { [3] = "safe" }; // 3월만 배정

            var chosen = DcaSettings.SelectTemplate(templates, monthMap, 5); // 5월은 미배정

            Assert.Null(chosen);
        }

        /// <summary>배정 Id가 존재하지 않는 템플릿을 가리키면 null(스킵)이어야 한다.</summary>
        [Fact]
        public void SelectTemplate_존재하지않는Id배정은_스킵한다()
        {
            var templates = new List<DcaTemplate> { Tpl("aggr"), Tpl("safe") };
            var monthMap = new Dictionary<int, string> { [3] = "ghost" }; // 없는 Id

            var chosen = DcaSettings.SelectTemplate(templates, monthMap, 3);

            Assert.Null(chosen);
        }

        /// <summary>템플릿 목록이 비어 있으면 null(스킵)이어야 한다.</summary>
        [Fact]
        public void SelectTemplate_템플릿이_없으면_스킵한다()
        {
            var templates = new List<DcaTemplate>();
            var monthMap = new Dictionary<int, string>();

            var chosen = DcaSettings.SelectTemplate(templates, monthMap, 1);

            Assert.Null(chosen);
        }
    }
}
