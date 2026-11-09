using System;
using UnityEngine;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void AdjustMaxPlayers(int delta)
        {
            _createMaxPlayers = Mathf.Clamp(_createMaxPlayers + (delta * playerStep), minPlayers, maxPlayersLimit);
            if (sliderMaxPlayers != null && Mathf.RoundToInt(sliderMaxPlayers.value) != _createMaxPlayers)
            {
                sliderMaxPlayers.value = _createMaxPlayers;
            }
            UpdateMaxPlayersDisplay();
        }

        private void OnMaxPlayersChanged(float val)
        {
            _createMaxPlayers = Mathf.Clamp(Mathf.RoundToInt(val), minPlayers, maxPlayersLimit);
            UpdateMaxPlayersDisplay();
        }

        private void OnMaxPlayersInputEndEdit(string text)
        {
            if (int.TryParse(text, out int val) && val > 0)
            {
                _createMaxPlayers = Mathf.Clamp(val, minPlayers, maxPlayersLimit);
                if (sliderMaxPlayers != null) sliderMaxPlayers.value = _createMaxPlayers;
            }
            UpdateMaxPlayersDisplay();
        }

        private void UpdateMaxPlayersDisplay()
        {
            string numStr = _createMaxPlayers.ToString();
            if (inputMaxPlayers != null && inputMaxPlayers.text != numStr)
            {
                inputMaxPlayers.text = numStr;
            }
            else if (labelMaxPlayers != null)
            {
                labelMaxPlayers.text = numStr;
            }
            if (labelMaxPlayersUnit != null) labelMaxPlayersUnit.text = "PLAYERS";
        }

        private void AdjustDuration(int delta)
        {
            _hasCustomizedDuration = true;
            _createDurationMinutes = Mathf.Clamp(_createDurationMinutes + (delta * durationStepMinutes), minDurationMinutes, maxDurationMinutes);
            if (sliderMatchDuration != null && Mathf.RoundToInt(sliderMatchDuration.value) != _createDurationMinutes)
            {
                sliderMatchDuration.value = _createDurationMinutes;
            }
            UpdateDurationDisplay();
        }

        private void OnDurationChanged(float val)
        {
            _hasCustomizedDuration = true;
            _createDurationMinutes = Mathf.Clamp(Mathf.RoundToInt(val), minDurationMinutes, maxDurationMinutes);
            UpdateDurationDisplay();
        }

        private void OnDurationInputEndEdit(string text)
        {
            if (int.TryParse(text, out int val) && val > 0)
            {
                _hasCustomizedDuration = true;
                _createDurationMinutes = Mathf.Clamp(val, minDurationMinutes, maxDurationMinutes);
                if (sliderMatchDuration != null) sliderMatchDuration.value = _createDurationMinutes;
            }
            UpdateDurationDisplay();
        }

        private void UpdateDurationDisplay()
        {
            string numStr = _createDurationMinutes.ToString();
            if (inputMatchDuration != null && inputMatchDuration.text != numStr)
            {
                inputMatchDuration.text = numStr;
            }
            else if (labelMatchDuration != null)
            {
                labelMatchDuration.text = numStr;
            }
            if (labelDurationUnit != null) labelDurationUnit.text = "MINS";
        }

        private void AdjustScoreLimit(int delta)
        {
            _hasCustomizedScore = true;
            _createScoreLimit = Mathf.Clamp(_createScoreLimit + (delta * scoreLimitStep), minScoreLimit, maxScoreLimit);
            if (sliderScoreLimit != null && Mathf.RoundToInt(sliderScoreLimit.value) != _createScoreLimit)
            {
                sliderScoreLimit.value = _createScoreLimit;
            }
            UpdateScoreLimitDisplay();
        }

        private void OnScoreLimitChanged(float val)
        {
            _hasCustomizedScore = true;
            _createScoreLimit = Mathf.Clamp(Mathf.RoundToInt(val), minScoreLimit, maxScoreLimit);
            UpdateScoreLimitDisplay();
        }

        private void OnScoreLimitInputEndEdit(string text)
        {
            if (int.TryParse(text, out int val) && val > 0)
            {
                _hasCustomizedScore = true;
                _createScoreLimit = Mathf.Clamp(val, minScoreLimit, maxScoreLimit);
                if (sliderScoreLimit != null) sliderScoreLimit.value = _createScoreLimit;
            }
            UpdateScoreLimitDisplay();
        }

        private void UpdateScoreLimitDisplay()
        {
            string numStr = _createScoreLimit.ToString();
            if (inputScoreLimit != null && inputScoreLimit.text != numStr)
            {
                inputScoreLimit.text = numStr;
            }
            else if (labelScoreLimit != null)
            {
                labelScoreLimit.text = numStr;
            }
            string unit = (_selectedCreateMode == "TDM") ? "PTS" : "KILLS";
            if (labelScoreLimitUnit != null) labelScoreLimitUnit.text = unit;
        }
    }
}
