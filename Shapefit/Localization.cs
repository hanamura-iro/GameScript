using System;
using System.Collections.Generic;
using UnityEngine;

public static class Localization
{
    public enum Lang { KR, EN }

    private const string LangKey = "ShapeFit_Lang";

    public static Lang Current { get; private set; } = (Lang)PlayerPrefs.GetInt(LangKey, (int)Lang.EN);

    public static event Action OnLanguageChanged;

    public static void SetLanguage(Lang lang)
    {
        if (Current == lang) return;
        Current = lang;
        PlayerPrefs.SetInt(LangKey, (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static void ToggleLanguage() => SetLanguage(Current == Lang.KR ? Lang.EN : Lang.KR);

    private static readonly Dictionary<string, (string kr, string en)> Table = new Dictionary<string, (string, string)>
    {
        { "score", ("점수", "Score") },
        { "best", ("최고", "Best") },
        { "stage", ("스테이지", "Stage") },
        { "time", ("시간", "Time") },
        { "combo", ("콤보!", "Combo!") },
        { "rotate", ("회전", "Rotate") },
        { "restart", ("다시 시작", "Restart") },
        { "gameover_title", ("게임 오버", "Game Over") },
        { "stageclear", ("스테이지 클리어!", "Stage Clear!") },
        { "settings", ("설정", "Settings") },
        { "close", ("닫기", "Close") },
        { "sound", ("사운드", "Sound") },
        { "on", ("켜짐", "On") },
        { "off", ("꺼짐", "Off") },
        { "language", ("언어", "Language") },
        { "total_cleared", ("누적 클리어", "Total Cleared") },
        { "best_combo", ("최다 콤보", "Best Combo") },
        { "games_played", ("플레이 횟수", "Games Played") },
        { "watch_ad_time", ("광고: +시간", "Ad: +Time") },
        { "watch_ad_revive", ("광고보고 부활", "Watch Ad to Revive") },
        { "watch_ad_hint", ("광고: 힌트", "Ad: Hint") },
        { "hint_free", ("힌트 (무료)", "Hint (Free)") },
        { "quit", ("게임 종료", "Quit Game") },
        { "cell_size", ("화면 크기", "Zoom") },
        { "hint_unavailable", ("다른 조각이 정답 자리를 막고 있어서 힌트를 알려줄 수 없어요.\n조각을 다시 빼서 재배치해보세요.", "Another piece is blocking the correct spot, so no hint is available.\nTry picking up a piece and rearranging.") },
        { "time_attack", ("타임어택", "Time Attack") },
        { "normal_mode", ("일반모드", "Normal Mode") },
        { "main_menu", ("메인으로", "Main Menu") },
        { "normal_desc", ("타이머 없이 느긋하게 즐기세요!", "No timer — play at your own pace") },
        { "time_attack_desc", ("제한시간 안에 빠르게! 콤보로 배율 UP", "Race the clock! Chain combos for bonus points") },
        { "big_board", ("빅보드", "Big Board") },
        { "big_board_desc", ("10x12 큰 보드, 20개 조각부터 시작!", "Huge 10x12 board, starts with 20 pieces!") },
        { "color_match", ("컬러매치", "Color Match") },
        { "color_match_desc", ("칸마다 정해진 색 조각만 놓을 수 있어요", "Each cell only accepts its matching color") },
        { "custom_mode", ("커스텀", "Custom") },
        { "custom_desc", ("시작 블록수/증가 간격을 직접 설정!", "Set your own starting pieces & pace!") },
        { "continue_run", ("이어하기", "Continue") },
        { "restart_run", ("처음부터", "Start Over") },
        { "custom_settings_label", ("커스텀 모드 설정", "Custom Mode Settings") },
        { "custom_base", ("시작 블록수", "Starting Pieces") },
        { "custom_interval", ("증가 간격(스테이지)", "Pace (stages)") },
        { "start_run", ("시작하기", "Start") },
        { "max_blocks", ("최대 블록", "Max Blocks") },
    };

    public static string Get(string key) => Table.TryGetValue(key, out var v) ? (Current == Lang.KR ? v.kr : v.en) : key;
}
