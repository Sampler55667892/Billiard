﻿namespace Assets.Scripts
{
    /// <summary>
    /// システム共通の定数
    /// </summary>
    public static class Constants
    {
        // MouseButtonIds
        public const int mouseLeftButtonId = 0;
        public const int mouseRightButtonId = 1;

        // Tags
        public const string ballToShotTag = "BallToShot";
        public const string ballOtherTag = "BallOther";
        public const string ball9Tag = "Ball9";
        public const string mainBoardTag = "MainBoard";

        // Server settings
        public const string serverHost = "127.0.0.1";
        public const int serverPort = 8080;

        // Server messages
        public const string srvm_activeTurn = "active turn";
        public const string srvm_notActiveTurn = "not active turn";
    }
}
