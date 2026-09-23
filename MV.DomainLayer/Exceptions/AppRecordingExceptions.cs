namespace MV.DomainLayer.Exceptions
{
    /// <summary>App gửi số thứ tự đoạn không hợp lệ.</summary>
    public class AppRecordingInvalidPartException : BadRequestException
    {
        public AppRecordingInvalidPartException()
            : base("Số thứ tự đoạn ghi âm không hợp lệ.") { }
    }

    /// <summary>Bản ghi đã chốt hoặc đã bỏ — không nhận thêm dữ liệu nữa.</summary>
    public class AppRecordingClosedException : BadRequestException
    {
        public AppRecordingClosedException()
            : base("Bản ghi này đã đóng, không nhận thêm dữ liệu.") { }
    }

    /// <summary>Bấm kết thúc nhưng chưa đoạn nào lên tới kho.</summary>
    public class AppRecordingEmptyException : BadRequestException
    {
        public AppRecordingEmptyException()
            : base("Chưa có đoạn ghi âm nào được tải lên.") { }
    }

    /// <summary>Báo cáo đã gửi cho phụ huynh rồi thì không rút lại bản ghi được.</summary>
    public class AppRecordingAlreadySentException : BadRequestException
    {
        public AppRecordingAlreadySentException()
            : base("Báo cáo đã gửi, không bỏ bản ghi được nữa.") { }
    }

    public class RecorderNotFoundException : NotFoundException
    {
        public RecorderNotFoundException(string message) : base(message) { }
    }

    /// <summary>Báo cáo chưa sẵn sàng để duyệt (AI chưa xong / bản ghi đã huỷ).</summary>
    public class RecorderNotReadyException : BadRequestException
    {
        public RecorderNotReadyException(string message) : base(message) { }
    }

    /// <summary>Link mời phụ huynh không dùng được (hết hạn, đã dùng, đã thu hồi).</summary>
    public class RecorderInviteInvalidException : BadRequestException
    {
        public RecorderInviteInvalidException(string message) : base(message) { }
    }

    /// <summary>Không xác minh được tài khoản Zalo của phụ huynh.</summary>
    public class RecorderZaloVerifyException : BadRequestException
    {
        public RecorderZaloVerifyException(string message) : base(message) { }
    }
}
