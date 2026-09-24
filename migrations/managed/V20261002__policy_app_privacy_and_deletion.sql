-- =====================================================
-- V20261002 — Văn bản pháp lý riêng cho ứng dụng Tutora dành cho gia sư (Google Play).
--   * privacy-app   : Chính sách quyền riêng tư của app ghi âm (VI + EN). App mở
--                     tutora.vn/policies/privacy-app; màn đăng ký trong app ghi đồng ý
--                     theo slug này (user_policy_acceptances).
--   * data-deletion : Trang yêu cầu xoá tài khoản / dữ liệu — Google Play bắt buộc có
--                     đường xoá dữ liệu trên web, không cần cài app.
--   * Chính sách chung của web ('privacy') viết cho bản marketplace cũ — chưa sửa ở đây.
-- ON CONFLICT (slug) DO NOTHING: chạy lại không ghi đè bản admin đã chỉnh qua CMS.
-- Nội dung đã xác nhận: ghi âm + bản chép lời lưu 180 ngày; gia sư không nghe lại;
-- xoá tài khoản: khoá ngay, dọn dữ liệu sau 30 ngày.
-- =====================================================
BEGIN;

INSERT INTO policy_documents (slug, title, summary, version, effective_date, status, display_order, published_at, content_markdown)
VALUES (
    'privacy-app',
    'Chính sách quyền riêng tư – Ứng dụng Tutora cho gia sư',
    'Ứng dụng ghi âm buổi học thu thập gì, AI xử lý thế nào, gửi báo cáo cho phụ huynh qua Zalo, lưu bao lâu và cách xoá dữ liệu.',
    '1.0',
    DATE '2026-10-01',
    'published',
    20,
    CURRENT_TIMESTAMP,
$pol$**Phiên bản:** v1 · **Ngày hiệu lực:** 01/10/2026 · **Áp dụng cho:** ứng dụng Android "Tutora" dành cho gia sư

### 1.1. Chúng tôi là ai

Ứng dụng Tutora ("Tutora", "chúng tôi") do **Dream Lab AI** vận hành và phát hành trên Google Play. Dream Lab AI là bên quyết định mục đích và cách thức xử lý dữ liệu cá nhân được mô tả trong chính sách này.

- Email hỗ trợ & quyền riêng tư: tutoravn@gmail.com
- Trang web: tutora.vn

### 1.2. Phạm vi

Chính sách này giải thích Tutora thu thập, sử dụng, chia sẻ, lưu giữ và bảo vệ dữ liệu cá nhân như thế nào khi:

- gia sư dùng ứng dụng Tutora trên Android;
- phụ huynh nhận báo cáo buổi học do Tutora gửi qua Zalo;
- bất kỳ ai gửi yêu cầu về dữ liệu tại tutora.vn/policies/data-deletion hoặc qua email hỗ trợ.

Ứng dụng là **công cụ làm việc chỉ dành cho gia sư từ đủ 18 tuổi**. Học sinh và phụ huynh không đăng nhập hay sử dụng ứng dụng. Chính sách này không áp dụng cho dịch vụ của bên thứ ba (như Zalo hoặc Google), vốn có chính sách riêng.

### 1.3. Dữ liệu chúng tôi thu thập

**a) Tài khoản gia sư** — do bạn cung cấp
- Họ tên, số điện thoại hoặc email, mật khẩu (nếu đăng ký bằng email; mật khẩu được lưu ở dạng băm, chúng tôi không xem được).
- Mã OTP khi đăng nhập bằng số điện thoại (gửi qua Zalo), chỉ dùng để xác minh.
- Thông tin hồ sơ bạn tự nhập thêm (nếu có).

**b) Thông tin học sinh và phụ huynh** — do gia sư nhập
- Học sinh: họ tên, lớp, môn học.
- Phụ huynh/người giám hộ: họ tên, số điện thoại (để nhận báo cáo qua Zalo).
- Bản ghi xác nhận đồng ý: phụ huynh đồng ý hay chưa, phiên bản văn bản đồng ý (ví dụ "v1"), thời điểm gia sư xác nhận, tài khoản gia sư xác nhận.

**c) Bản ghi âm buổi học**
- Âm thanh được ghi bằng micro của điện thoại **chỉ khi gia sư chủ động bấm ghi âm** cho một buổi học. Bản ghi có thể chứa giọng nói của gia sư, học sinh và người khác có mặt.

**d) Nội dung do AI tạo và gia sư chỉnh sửa**
- Báo cáo buổi học ngắn gửi phụ huynh (nội dung đã học, bài tập về nhà, ghi chú — mỗi mục tối đa 200 ký tự).
- Biên bản buổi học chỉ gia sư xem.
- Bản chép lời đã ẩn danh: tên, số điện thoại, địa chỉ, tên trường được thay bằng ký hiệu thay thế; dùng làm dữ liệu nội bộ.
- Trạng thái duyệt và gửi báo cáo (đã duyệt, đã gửi, gửi lỗi).

**e) Dữ liệu thiết bị và ứng dụng**
- Mã thông báo đẩy (Firebase Cloud Messaging token) để gửi thông báo.
- Thông tin kỹ thuật cơ bản: loại thiết bị, phiên bản Android, phiên bản ứng dụng, nhật ký lỗi và nhật ký truy cập máy chủ (địa chỉ IP, thời gian) phục vụ vận hành và bảo mật.
- Chúng tôi **không** truy cập danh bạ, vị trí, ảnh hay tệp khác trên máy của bạn.

### 1.4. Mục đích và cơ sở xử lý

| Mục đích | Dữ liệu | Cơ sở xử lý |
|---|---|---|
| Tạo và quản lý tài khoản gia sư, đăng nhập, bảo mật tài khoản | Tài khoản gia sư, OTP, dữ liệu thiết bị | Thực hiện thỏa thuận sử dụng dịch vụ với gia sư; sự đồng ý của gia sư |
| Quản lý danh sách học sinh của gia sư | Thông tin học sinh, phụ huynh | Sự đồng ý của phụ huynh/người giám hộ, do gia sư thu nhận và xác nhận trong ứng dụng |
| Ghi âm, tạo báo cáo và biên bản bằng AI | Bản ghi âm, nội dung AI | Sự đồng ý của phụ huynh/người giám hộ; sự đồng ý của gia sư |
| Gửi báo cáo đã được gia sư duyệt tới phụ huynh qua Zalo | Tên học sinh, SĐT phụ huynh, nội dung báo cáo | Sự đồng ý của phụ huynh/người giám hộ |
| Gửi thông báo trong ứng dụng cho gia sư | FCM token | Thực hiện thỏa thuận sử dụng dịch vụ |
| Cải thiện chất lượng báo cáo và tính năng | Bản chép lời đã ẩn danh | Sự đồng ý của phụ huynh/người giám hộ |
| Xử lý khiếu nại, yêu cầu xoá, bảo mật hệ thống, tuân thủ pháp luật | Dữ liệu liên quan tới yêu cầu, nhật ký truy cập | Nghĩa vụ pháp lý; bảo vệ quyền và lợi ích hợp pháp |

Chúng tôi **không** bán dữ liệu cá nhân, **không** hiển thị quảng cáo và **không** dùng dữ liệu để tạo hồ sơ quảng cáo.

### 1.5. Xử lý bằng trí tuệ nhân tạo (AI)

- Bản ghi âm được gửi tới **Google Gemini API** (gói trả phí) để chép lời và tạo bản nháp báo cáo, biên bản.
- Theo điều khoản gói trả phí của Google, dữ liệu gửi qua API **không được dùng để huấn luyện mô hình của Google**. Google có thể lưu tạm trong thời gian giới hạn để phát hiện lạm dụng theo điều khoản của họ.
- Nội dung do AI tạo có thể sai hoặc thiếu. Vì vậy **không có báo cáo nào được gửi tự động**: gia sư phải xem, chỉnh sửa (nếu cần) và bấm duyệt trước khi Tutora gửi đến phụ huynh. Gia sư chịu trách nhiệm về nội dung đã duyệt.
- AI không đưa ra quyết định có ảnh hưởng pháp lý hay tương tự đối với học sinh, phụ huynh hoặc gia sư.
- Phụ huynh hoặc gia sư có thể báo báo cáo sai qua tutoravn@gmail.com.

### 1.6. Chia sẻ dữ liệu và bên xử lý

Chúng tôi chỉ chia sẻ dữ liệu với các nhà cung cấp dịch vụ cần thiết để vận hành ứng dụng, theo hợp đồng/điều khoản dịch vụ ràng buộc họ bảo mật dữ liệu:

| Bên nhận | Vai trò | Dữ liệu | Nơi xử lý |
|---|---|---|---|
| Supabase | Lưu trữ cơ sở dữ liệu và tệp ghi âm (bucket riêng tư, mã hoá khi truyền và khi lưu) | Toàn bộ dữ liệu ứng dụng | Singapore |
| Google (Gemini API) | Chép lời và tạo báo cáo bằng AI | Bản ghi âm, lời chép | Hạ tầng toàn cầu của Google (có thể ngoài Việt Nam) |
| Google (Firebase Cloud Messaging) | Gửi thông báo đẩy | FCM token, nội dung thông báo | Hạ tầng toàn cầu của Google |
| VNG (Zalo) | Gửi mã OTP đăng nhập; gửi báo cáo tới phụ huynh bằng tin nhắn mẫu (ZBS) từ Zalo Official Account của Tutora | SĐT, nội dung OTP/báo cáo, tên học sinh | Việt Nam |

Ngoài ra, chúng tôi có thể cung cấp dữ liệu khi cơ quan nhà nước có thẩm quyền yêu cầu theo quy định pháp luật, hoặc cho bên tiếp nhận trong trường hợp tái cấu trúc/chuyển giao doanh nghiệp (khi đó chúng tôi sẽ thông báo trước và bên nhận phải tuân thủ chính sách này).

**Chuyển dữ liệu ra nước ngoài.** Vì máy chủ lưu trữ đặt tại Singapore và dịch vụ AI của Google có thể xử lý ở ngoài Việt Nam, dữ liệu cá nhân của bạn sẽ được chuyển ra nước ngoài. Chúng tôi lập và lưu hồ sơ đánh giá tác động chuyển dữ liệu theo quy định pháp luật Việt Nam, chỉ chuyển dữ liệu cần thiết cho mục đích nêu trên và áp dụng mã hoá khi truyền.

### 1.7. Dữ liệu của trẻ em và sự đồng ý của phụ huynh

- Học sinh trong ứng dụng thường từ 6 đến 17 tuổi. Trẻ em không sử dụng ứng dụng; chỉ gia sư đủ 18 tuổi mới được tạo tài khoản.
- **Trước khi ghi âm**, gia sư phải giải thích cho phụ huynh/người giám hộ nội dung văn bản đồng ý của Tutora (việc ghi âm, tóm tắt bằng AI, gửi báo cáo qua Zalo, thời hạn xoá bản ghi) và xác nhận trong ứng dụng rằng phụ huynh đã đồng ý. Ứng dụng không cho ghi âm nếu chưa có xác nhận này.
- Chúng tôi lưu phiên bản văn bản đồng ý và thời điểm xác nhận làm bằng chứng. Khi nội dung đồng ý thay đổi đáng kể, gia sư phải xin lại sự đồng ý theo phiên bản mới.
-
- Phụ huynh/người giám hộ có thể rút lại sự đồng ý bất kỳ lúc nào (xem mục 1.10). Khi rút lại, việc ghi âm và gửi báo cáo cho học sinh đó sẽ dừng.
- Nếu phát hiện dữ liệu của trẻ em được thu thập mà không có sự đồng ý hợp lệ, chúng tôi sẽ dừng xử lý và xoá dữ liệu đó.

### 1.8. Thời gian lưu trữ

| Loại dữ liệu | Thời gian lưu |
|---|---|
| Bản ghi âm buổi học | Tự động xoá **180 ngày** sau ngày ghi. Gia sư không nghe lại được; chỉ quản trị viên Tutora được nghe khi xử lý khiếu nại |
| Bản chép lời đã ẩn danh | **180 ngày** sau ngày ghi, sau đó xoá |
| Báo cáo và biên bản buổi học | Trong thời gian tài khoản gia sư còn hoạt động; xoá trong vòng 30 ngày sau khi tài khoản bị xoá (hoặc sớm hơn nếu có yêu cầu xoá hợp lệ) |
| Thông tin học sinh, phụ huynh | Như báo cáo; hoặc tới khi gia sư xoá học sinh / phụ huynh yêu cầu xoá |
| Tài khoản gia sư | Tới khi gia sư xoá tài khoản; xoá trong vòng 30 ngày sau đó |
| Bản ghi xác nhận đồng ý | Lưu làm bằng chứng trong thời hạn pháp luật yêu cầu, kể cả sau khi rút lại đồng ý hoặc xoá tài khoản |
| Nhật ký máy chủ, bảo mật | 90 ngày |
| Tin nhắn Zalo phụ huynh đã nhận | Nằm trong ứng dụng Zalo của phụ huynh; Tutora không xoá được, phụ huynh có thể tự xoá |

### 1.9. Bảo mật

- Dữ liệu được mã hoá khi truyền (HTTPS/TLS) và khi lưu trữ.
- Tệp ghi âm nằm trong kho lưu trữ riêng tư, không có đường dẫn công khai.
- **Gia sư không nghe lại được bản ghi âm trong ứng dụng.** Chỉ quản trị viên kỹ thuật được Tutora ủy quyền mới truy cập bản ghi, và chỉ để xử lý khiếu nại hoặc yêu cầu xoá; mọi lần truy cập đều được ghi nhật ký.
- Quyền truy cập hệ thống được cấp theo nguyên tắc tối thiểu cần thiết.
- Nếu xảy ra sự cố lộ lọt dữ liệu, chúng tôi sẽ thông báo cho cơ quan có thẩm quyền và người bị ảnh hưởng theo thời hạn pháp luật quy định.

Không có hệ thống nào an toàn tuyệt đối; bạn nên bảo vệ điện thoại và mật khẩu của mình.

### 1.10. Quyền của bạn

Theo pháp luật Việt Nam về bảo vệ dữ liệu cá nhân, gia sư, phụ huynh/người giám hộ (thay mặt học sinh) có quyền:

- **Được biết** dữ liệu nào được xử lý, vì sao và bởi ai;
- **Truy cập** và nhận bản sao dữ liệu;
- **Chỉnh sửa** dữ liệu sai hoặc thiếu;
- **Xoá** dữ liệu;
- **Rút lại sự đồng ý** (không ảnh hưởng tới việc xử lý đã thực hiện trước đó);
- **Hạn chế** hoặc **phản đối** việc xử lý;
- Khiếu nại, tố cáo, yêu cầu bồi thường theo quy định pháp luật.

**Cách thực hiện:**
- Gia sư: sửa thông tin trong ứng dụng; xoá học sinh trong danh sách; xoá tài khoản tại **Hồ sơ → Xoá tài khoản**.
- Phụ huynh: gửi yêu cầu tại **tutora.vn/policies/data-deletion**, qua tutoravn@gmail.com, hoặc nhờ gia sư của con.
- Chúng tôi có thể cần xác minh danh tính (ví dụ gửi mã xác nhận tới số điện thoại đã đăng ký) trước khi xử lý.
- Thời hạn phản hồi: trong thời hạn pháp luật quy định, cụ thể trong vòng **20 ngày** với yêu cầu xoá (tối đa 30 ngày nếu cần phối hợp với bên xử lý) và **10 ngày** với yêu cầu truy cập/chỉnh sửa.

Nếu không hài lòng với cách chúng tôi xử lý, bạn có thể gửi khiếu nại tới cơ quan chuyên trách bảo vệ dữ liệu cá nhân (Bộ Công an).

### 1.11. Xoá tài khoản và dữ liệu

- **Trong ứng dụng:** Hồ sơ → Xoá tài khoản → xác nhận. Tài khoản bị vô hiệu ngay; dữ liệu liên quan (hồ sơ, danh sách học sinh, báo cáo, biên bản, bản ghi âm còn lại) được xoá trong vòng 30 ngày.
- **Trên web (không cần ứng dụng):** tutora.vn/policies/data-deletion.
- Dữ liệu có thể được giữ lại sau khi xoá: bản ghi xác nhận đồng ý (làm bằng chứng pháp lý) và nhật ký bảo mật trong thời hạn nêu ở mục 1.8; bản sao lưu được ghi đè theo chu kỳ tối đa 30 ngày.

### 1.12. Thay đổi chính sách

Chúng tôi có thể cập nhật chính sách này. Mỗi phiên bản ghi rõ số phiên bản và ngày hiệu lực; các phiên bản cũ được lưu tại. Với thay đổi quan trọng (ví dụ thêm mục đích xử lý mới hoặc bên nhận mới), chúng tôi sẽ thông báo trong ứng dụng trước khi áp dụng và xin lại sự đồng ý khi pháp luật yêu cầu.

### 1.13. Liên hệ

Dream Lab AI — đơn vị vận hành Tutora
Yêu cầu về dữ liệu: tutora.vn/policies/data-deletion

---

## English version

**Version:** v1 · **Effective date:** 01/10/2026 · **Applies to:** the "Tutora" Android app for tutors

*If the Vietnamese and English versions differ, the Vietnamese version prevails.*

#### 1.1. Who we are

The Tutora app ("Tutora", "we", "us") is operated and published on Google Play by **Dream Lab AI**. Dream Lab AI decides why and how the personal data described in this policy is processed.

- Support & privacy email: tutoravn@gmail.com
- Website: tutora.vn

#### 1.2. Scope

This policy explains how Tutora collects, uses, shares, retains and protects personal data when:

- tutors use the Tutora Android app;
- parents receive lesson reports sent by Tutora via Zalo;
- anyone submits a data request at tutora.vn/policies/data-deletion or by email.

The app is a **work tool for tutors aged 18 or older only**. Students and parents do not sign in to or use the app. Third-party services (such as Zalo or Google) have their own policies, which this policy does not cover.

#### 1.3. Data we collect

**a) Tutor account** — provided by you
- Name, phone number or email, password (email sign-up only; stored hashed, we cannot read it).
- One-time passcodes (OTP) delivered via Zalo for phone sign-in, used only for verification.
- Any additional profile details you choose to add.

**b) Student and parent information** — entered by the tutor
- Student: name, grade, subject.
- Parent/guardian: name, phone number (to receive reports on Zalo).
- Consent record: whether the parent agreed, the consent text version (e.g. "v1"), when the tutor confirmed it, and which tutor account confirmed it.

**c) Lesson audio**
- Audio is recorded through the phone microphone **only when the tutor starts a recording** for a lesson. It may contain the voices of the tutor, the student and anyone else present.

**d) AI-generated content edited by the tutor**
- A short lesson report for the parent (lesson content, homework, notes — each up to 200 characters).
- Session minutes visible only to the tutor.
- An anonymized transcript in which names, phone numbers, addresses and school names are replaced with placeholders, kept as internal material.
- Review and delivery status (approved, sent, failed).

**e) Device and app data**
- A push-notification token (Firebase Cloud Messaging).
- Basic technical data: device model, Android version, app version, error logs and server access logs (IP address, time) for operations and security.
- We do **not** access your contacts, location, photos or other files.

#### 1.4. Purposes and legal bases

| Purpose | Data | Legal basis |
|---|---|---|
| Create and manage tutor accounts, sign-in, account security | Tutor account, OTP, device data | Performance of our service agreement with the tutor; tutor's consent |
| Manage the tutor's student list | Student and parent information | Consent of the parent/guardian, obtained and confirmed in-app by the tutor |
| Record lessons and generate AI reports and minutes | Audio, AI content | Consent of the parent/guardian; tutor's consent |
| Send tutor-approved reports to parents via Zalo | Student name, parent phone, report content | Consent of the parent/guardian |
| Send in-app notifications to tutors | FCM token | Performance of the service agreement |
| Improve report quality and features | Anonymized transcripts | Consent of the parent/guardian |
| Handle complaints and deletion requests, secure the system, comply with law | Request data, access logs | Legal obligations; protection of legitimate rights and interests |

We do **not** sell personal data, show ads, or build advertising profiles.

#### 1.5. AI processing

- Audio is sent to the **Google Gemini API** (paid tier) to transcribe it and draft the report and minutes.
- Under Google's paid-tier terms, data sent through the API is **not used to train Google's models**. Google may keep it for a limited time to detect abuse, as set out in its terms.
- AI output can be wrong or incomplete. **No report is sent automatically**: the tutor must review, edit if needed, and approve it before Tutora sends it to the parent. The tutor is responsible for approved content.
- AI does not make decisions with legal or similarly significant effects on students, parents or tutors.
- Parents or tutors can report an inaccurate report at tutoravn@gmail.com.

#### 1.6. Sharing and service providers

We share data only with the providers needed to run the app, under contracts or service terms that bind them to protect it:

| Recipient | Role | Data | Location |
|---|---|---|---|
| Supabase | Database and audio storage (private bucket, encrypted in transit and at rest) | All app data | Singapore |
| Google (Gemini API) | AI transcription and report drafting | Audio, transcripts | Google global infrastructure (may be outside Vietnam) |
| Google (Firebase Cloud Messaging) | Push notifications | FCM token, notification content | Google global infrastructure |
| VNG (Zalo) | Sign-in OTP delivery; sending reports to parents as template messages (ZBS) from Tutora's Zalo Official Account | Phone number, OTP/report content, student name | Vietnam |

We may also disclose data when a competent authority requires it by law, or to a successor in a restructuring or business transfer (we will give notice beforehand and the successor must honor this policy).

**Transfers outside Vietnam.** Because our storage servers are in Singapore and Google's AI services may process data outside Vietnam, your personal data will be transferred abroad. We prepare and keep a transfer impact assessment as required by Vietnamese law, transfer only what the purposes above require, and encrypt data in transit.

#### 1.7. Children's data and parental consent

- Students are typically aged 6 to 17. Children do not use the app; only tutors aged 18+ can create accounts.
- **Before recording**, the tutor must explain Tutora's consent text to the parent/guardian (recording, AI summaries, reports via Zalo, when audio is deleted) and confirm in the app that the parent agreed. The app does not allow recording without this confirmation.
- We store the consent text version and confirmation time as proof. If the consent text changes materially, the tutor must obtain consent again under the new version.
-
- A parent/guardian can withdraw consent at any time (see section 1.10). Recording and reporting for that student then stop.
- If we learn that a child's data was collected without valid consent, we will stop processing and delete it.

#### 1.8. Retention

| Data | Retention |
|---|---|
| Lesson audio | Automatically deleted **180 days** after recording. Tutors cannot replay it; only Tutora administrators may listen when handling complaints |
| Anonymized transcript | **180 days** after recording, then deleted |
| Lesson reports and minutes | While the tutor account is active; deleted within 30 days after account deletion (or sooner on a valid deletion request) |
| Student and parent information | As for reports, or until the tutor removes the student / the parent asks for deletion |
| Tutor account | Until the tutor deletes it; erased within 30 days afterwards |
| Consent records | Kept as proof for the period required by law, including after consent is withdrawn or the account is deleted |
| Server and security logs | 90 days |
| Zalo messages already received by parents | Stored in the parent's Zalo app; Tutora cannot delete them, the parent can |

#### 1.9. Security

- Data is encrypted in transit (HTTPS/TLS) and at rest.
- Audio files are kept in private storage with no public links.
- **Tutors cannot replay audio in the app.** Only authorized Tutora technical administrators can access recordings, and only to handle complaints or deletion requests; every access is logged.
- System access follows the least-privilege principle.
- If a data breach occurs, we will notify the authorities and affected people within the time required by law.

No system is perfectly secure; please protect your phone and password.

#### 1.10. Your rights

Under Vietnamese personal data protection law, tutors and parents/guardians (on behalf of students) have the right to:

- **be informed** about what data is processed, why, and by whom;
- **access** and obtain a copy of their data;
- **correct** inaccurate or incomplete data;
- **delete** their data;
- **withdraw consent** (without affecting processing already carried out);
- **restrict** or **object to** processing;
- complain, report and claim compensation as provided by law.

**How to exercise them:**
- Tutors: edit details in the app; remove students from the list; delete the account under **Profile → Delete account** (Hồ sơ → Xoá tài khoản).
- Parents: submit a request at **tutora.vn/policies/data-deletion**, email tutoravn@gmail.com, or ask your child's tutor.
- We may need to verify identity (for example with a code sent to the registered phone number) before acting.
- Response time: within legal time limits — **20 days** for deletion (up to 30 days where service providers must be involved) and **10 days** for access/correction.

If you are unhappy with how we handle your request, you may complain to the competent personal data protection authority (Ministry of Public Security).

#### 1.11. Account and data deletion

- **In the app:** Profile → Delete account → confirm. The account is disabled immediately; associated data (profile, student list, reports, minutes, remaining audio) is erased within 30 days.
- **On the web (no app needed):** tutora.vn/policies/data-deletion.
- Data that may be kept after deletion: consent records (legal proof) and security logs for the periods in section 1.8; backups are overwritten on a rolling cycle of up to 30 days.

#### 1.12. Changes

We may update this policy. Each version shows a version number and effective date; previous versions are archived at. For material changes (such as a new purpose or new recipient), we will notify you in the app before they apply and ask for consent again where the law requires.

#### 1.13. Contact

Dream Lab AI — operator of Tutora
Data requests: tutora.vn/policies/data-deletion$pol$
)
ON CONFLICT (slug) DO NOTHING;

INSERT INTO policy_documents (slug, title, summary, version, effective_date, status, display_order, published_at, content_markdown)
VALUES (
    'data-deletion',
    'Yêu cầu xoá tài khoản và dữ liệu – Ứng dụng Tutora',
    'Cách gia sư và phụ huynh yêu cầu xoá tài khoản, dữ liệu học sinh và báo cáo; dữ liệu nào bị xoá, dữ liệu nào được giữ lại và trong bao lâu.',
    '1.0',
    DATE '2026-10-01',
    'published',
    21,
    CURRENT_TIMESTAMP,
$pol$Ứng dụng **Tutora** (nhà phát triển trên Google Play: **Dream Lab AI**) cho phép gia sư và phụ huynh yêu cầu xoá dữ liệu cá nhân. Bạn không cần cài ứng dụng để gửi yêu cầu trên trang này.

### Ai có thể gửi yêu cầu
- **Gia sư** có tài khoản Tutora: xoá toàn bộ tài khoản, hoặc xoá dữ liệu của một/một số học sinh.
- **Phụ huynh/người giám hộ** của học sinh được gia sư thêm vào Tutora: xoá dữ liệu của con, các báo cáo buổi học và rút lại sự đồng ý ghi âm.

### Cách 1 — Xoá ngay trong ứng dụng (dành cho gia sư)
1. Mở ứng dụng Tutora và đăng nhập.
2. Vào **Hồ sơ → Xoá tài khoản**.
3. Đọc thông tin và bấm **Xác nhận xoá**.

Tài khoản bị vô hiệu ngay lập tức. Để xoá riêng một học sinh: vào danh sách học sinh → chọn học sinh → **Xoá học sinh**.

### Cách 2 — Gửi yêu cầu qua email (không cần ứng dụng)

Gửi email tới **tutoravn@gmail.com** với tiêu đề **"Yêu cầu xoá dữ liệu"**, gồm:
- **Họ và tên**;
- **Số điện thoại hoặc email** dùng với Tutora (để xác minh và phản hồi);
- **Vai trò:** gia sư hoặc phụ huynh/người giám hộ;
- **Học sinh liên quan** (nếu là phụ huynh): họ tên học sinh, tên gia sư (nếu biết);
- **Nội dung yêu cầu:** xoá toàn bộ tài khoản, xoá dữ liệu của học sinh, rút lại sự đồng ý ghi âm, hoặc yêu cầu khác;
- Xác nhận bạn là chủ tài khoản hoặc phụ huynh/người giám hộ hợp pháp của học sinh nêu trên.

### Chúng tôi xử lý thế nào
1. Xác minh danh tính (ví dụ gửi mã xác nhận tới số điện thoại/email bạn cung cấp). Chúng tôi chỉ xoá khi xác minh được người yêu cầu.
2. Dừng ngay việc ghi âm và gửi báo cáo liên quan (khi phụ huynh rút lại đồng ý).
3. Hoàn tất xoá trong vòng **20 ngày** kể từ khi xác minh xong (tối đa 30 ngày nếu cần phối hợp với nhà cung cấp dịch vụ), và thông báo kết quả cho bạn.

### Dữ liệu bị xoá

| Dữ liệu | Khi gia sư xoá tài khoản | Khi phụ huynh yêu cầu xoá dữ liệu của con |
|---|---|---|
| Thông tin tài khoản gia sư | Xoá | Không áp dụng |
| Họ tên, lớp, môn của học sinh; tên, SĐT phụ huynh | Xoá | Xoá |
| Bản ghi âm còn lưu (tối đa 180 ngày) | Xoá | Xoá |
| Báo cáo và biên bản buổi học | Xoá | Xoá |
| Bản chép lời đã ẩn danh | Xoá | Xoá |

### Dữ liệu được giữ lại và thời hạn
- **Bản ghi xác nhận đồng ý** (phiên bản văn bản, thời điểm, tài khoản xác nhận): giữ làm bằng chứng tuân thủ trong, không dùng cho mục đích khác.
- **Nhật ký bảo mật và nhật ký xử lý yêu cầu:** 90 ngày.
- **Bản sao lưu:** tự động bị ghi đè trong tối đa 30 ngày.
- **Tin nhắn Zalo** phụ huynh đã nhận nằm trong ứng dụng Zalo của phụ huynh; Tutora không thể xoá, phụ huynh có thể tự xoá trong Zalo.

### Liên hệ
Chính sách quyền riêng tư: tutora.vn/policies/privacy-app

---

## English version

The **Tutora** app (Google Play developer: **Dream Lab AI**) lets tutors and parents request deletion of personal data. You do not need the app to submit a request on this page.

#### Who can request
- **Tutors** with a Tutora account: delete the whole account, or the data of one or more students.
- **Parents/guardians** of a student added to Tutora by a tutor: delete the child's data and lesson reports, and withdraw consent to recording.

#### Option 1 — Delete in the app (tutors)
1. Open Tutora and sign in.
2. Go to **Profile → Delete account** (Hồ sơ → Xoá tài khoản).
3. Read the notice and tap **Confirm deletion**.

The account is disabled immediately. To remove a single student: student list → select the student → **Delete student**.

#### Option 2 — Request by email (no app needed)

Email **tutoravn@gmail.com** with the subject **"Data deletion request"**, including:
- **Full name**;
- **Phone number or email** used with Tutora (for verification and reply);
- **Role:** tutor or parent/guardian;
- **Related student** (parents): student's name, tutor's name (if known);
- **Request:** delete the whole account, delete a student's data, withdraw recording consent, or other;
- Confirmation that you are the account holder or the lawful parent/guardian of the student.

#### How we handle it
1. Verify identity (for example with a code sent to the phone/email you give). We delete only once the requester is verified.
2. Immediately stop recording and report delivery concerned (when a parent withdraws consent).
3. Complete deletion within **20 days** of verification (up to 30 days where service providers must be involved), and tell you the outcome.

#### What is deleted

| Data | Tutor deletes account | Parent requests deletion of child's data |
|---|---|---|
| Tutor account details | Deleted | N/A |
| Student name, grade, subject; parent name and phone | Deleted | Deleted |
| Remaining audio (kept at most 180 days) | Deleted | Deleted |
| Lesson reports and minutes | Deleted | Deleted |
| Anonymized transcripts | Deleted | Deleted |

#### What is kept, and for how long
- **Consent records** (text version, time, confirming account): kept as compliance proof for, not used for anything else.
- **Security and request-handling logs:** 90 days.
- **Backups:** automatically overwritten within 30 days.
- **Zalo messages** already received by the parent stay in the parent's Zalo app; Tutora cannot delete them, the parent can.

#### Contact
Privacy policy: tutora.vn/policies/privacy-app$pol$
)
ON CONFLICT (slug) DO NOTHING;

COMMIT;
