namespace serial_communication_client{
    public enum CommandName {
        UNDEFINED = -1,
        NONE = 48,
        ALARM = 1,
        PRINT_CHAR = 49,
        PRINT_NUMBER =50, // 2
        LED_ON = 51, // 3
        LED_OFF = 52, // 4
        LED_BLINK = 54, // 6
        STOP_COMMAND = 53, // 5
        MOVE_SERVO = 56,
        READ_SERVO = 57
    }
}