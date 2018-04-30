# Sản phẩm có gì?
* Tính năng cơ bản của sản phẩm là cho phép Client và Server chat với nhau.
* Các thông điệp được mã hóa bằng AES, và khóa được sử dụng trong thuật toán AES được trao đổi qua lại giữa Client và Server bằng phương thức Diffie Hellman.
# Sử dụng làm sao?
Thật sự không quá khó khăn để sử dụng một ứng dụng GUI với giao diện đơn giản trực quan.

Để mô tả rõ hơn cách sử dụng phần mềm, ta sử dụng hai máy ảo là Windows 7 có IP là **10.0.0.2** và Windows 8.1 có IP là **10.0.0.16**, hai máy đều sử dụng chung card mạng là Host-only. Ta chọn máy Windows 8.1 là Server.

Sản phẩm được chia ra 2 phần là Server và Client với file thực thi và thư viện kèm theo (lưu ý là phải có file thư viện MessageLib.dll kèm theo). File thực thi và thư viện nằm trong thư mục [Release](https://github.com/arituan/private-chat-diffiehellman/tree/master/Release)

- Đầu tiên là chạy **Server.exe** lên ta có giao diện như sau:
	
	![Giao diện Server](https://github.com/arituan/private-chat-diffiehellman/raw/master/man-server.PNG)

	Ta tiến hành chọn IP và Port để lắng nghe các kết nối từ phía client (Danh sách IP được tạo dựa vào các Interface network trên máy), Ở đây ta chọn IP 10.0.0.16 với port là 42018. Sau đó nhấn nút **Mở kết nối** để bắt đầu lắng nghe.

- Tiếp theo khởi chạy file **Client.exe** ta được giao diện như hình sau:
	
	![Giao diện Client](https://github.com/arituan/private-chat-diffiehellman/raw/master/man-client.PNG)

	Điền IP và Port của Server và ấn **Kết nối**.
	
- Sau khi 2 bên tạo kết nối thành công, ta bắt đầu trò chuyện giữa Client và Server. Lưu ý trước khi tạo kết nối ta nên bật wireshark để theo dõi quá trình trao đổi giữa Client và Server.
	
	Kết quả màn hình tại Server:
	
	![Kết quả tại Server](https://github.com/arituan/private-chat-diffiehellman/raw/master/server-capture.PNG)
	
	Kết quả màn hình tại Client:
	
	![Kết quả tại Client](https://github.com/arituan/private-chat-diffiehellman/raw/master/client-capture.PNG)

- Kết quả Wireshark bắt được:
	
	![Kết quả Wireshark](https://github.com/arituan/private-chat-diffiehellman/raw/master/wireshark-capture.PNG)

	Từ kết quả Wireshark bắt được, ta chỉ nhìn được cấu trúc của các message chứ không đọc được nội dung cụ thể bên trong, vì nó đã được mã hóa.

# Làm như thế nào để viết được?
Project được thực hiện trên Visual Studio 2015.

Quá trình trao đổi giữa client và server sử dụng thư viện **System.Net.Socket** (tham khảo thêm tại: https://msdn.microsoft.com/en-us/library/system.net.sockets(v=vs.110).aspx)

Sau khi tạo kết nối thành công, giữa client và server sẽ diễn ra quá trình trao đổi khóa với nhau bằng phương thức Diffie Hellman, ở đây sử dụng thư viện có sẵn trong .NET là **System.Security.Cryptography**, trong thư viện này cung cấp cho chúng ta Class **ECDiffieHellmanCng**, class này sử dụng lí thuyết đường cong Elliptic. Cũng là ý tưởng trao đổi khóa Diffie Hellman nhưng với **Elliptic Curve Diffie Hellman** thì chỉ khác nhau ở việc sinh G so với Diffie Hellman truyền thống:

* Alice và Bob thống nhất với nhau một điểm **P** nằm trên đường cong **E** thuộc trường trường **F**<sub>q</sub>. Điểm **P** ở đây đóng vai trò như số sinh g trong Diffie Hellman truyền thống.
* Alice sẽ sinh ngẫu nhiên một số bí mật k<sub>A</sub>, và tính giá trị P<sub>A</sub>=k<sub>A</sub>P
* Bob sẽ sinh ngẫu nhiên một số bí mật k<sub>B</sub>, và tính giá trị P<sub>B</sub>=k<sub>B</sub>P
* Alice và Bob trao đổi cho nhau P<sub>A</sub> và P<sub>B</sub>.
* Alice và Bob sẽ có chung giá trị P<sub>AB</sub> = k<sub>A</sub>P<sub>B</sub> = k<sub>B</sub>P<sub>A</sub> = k<sub>A</sub>k<sub>B</sub>P.
	
	Các số bí mật k<sub>A</sub> và k<sub>B</sub> được sinh ngẫu nhiên trong khoảng {1, ..., n-1}, trong đó n là thứ tự nhóm được sinh ra bởi P.

Alice và Bob lúc nãy đã có khóa bí mật để dùng chung, ta sẽ dùng khóa này vào một thuật toán mã hóa đối xứng, trong project sử dụng AES để mã hóa. Mã hóa AES cũng có sẵn trong thư viện **System.Security.Cryptography**. Hiện thực hóa quá trình trao đổi khóa và mã hóa qua ví dụ tại: https://msdn.microsoft.com/en-us/library/system.security.cryptography.ecdiffiehellmancng(v=vs.110).aspx

Ta nhìn ví dụ trong link sẽ thấy khi thực hiện mã hóa AES sẽ sinh ra một **IV**, IV này sẽ được kèm theo thông điệp và gửi qua cho đối phương. Một vấn đề ta cần giải quyết là làm sao tích hợp được IV vào thông điệp. Để giải quyết vấn đề đó thì cần tạo một cấu trúc cho thông điệp và sử dụng Serialization để gói gọn cấu trúc đó thành các bytes và truyền đi.

Đến đây thì một vấn đề nữa xảy ra là làm sao để phân biệt gói tin nào là gói tin trao đổi khóa, gói nào là gói thông điệp bình thường. Vậy ta cần thêm một thuộc tính là mode vào trong cấu trúc thông điệp. Cấu trúc của thông điệp như sau:

````Csharp
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

/*
* Structure a Message
* Attributes
*      byte[] msg: contain message or public key
*      exData: IV in AES
*      mode: 0 - KeyExchange, 1 - SendMsg
*/

namespace MessageLib
{

	public class MsgStruct : ISerializable
	{
		
		private byte[] _msg;
		private int _mode;
		private byte[] _extData; //IV or Public key exchange
		
		public MsgStruct()
		{
			msg = null;
			extData = null;
			mode = 1;
			
		}
		
		public MsgStruct(byte[] txt, int mode)
		{
			this.msg = txt;
			this.mode = mode;
		}
		
		public MsgStruct(string msg)
		{
			if (msg != null)
			this.msg = Encoding.UTF8.GetBytes(msg);
		}
		
		public void Encrypt(byte[] key)
		{
			using (Aes aes = new AesCryptoServiceProvider())
			{
				aes.Key = key;
				extData = aes.IV;
				// Encrypt the message
				using (MemoryStream ciphertext = new MemoryStream())
				using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write))
				{
					cs.Write(msg, 0, msg.Length);
					cs.Close();
					msg = ciphertext.ToArray();
				}
			}
		}
		
		public void Decrypt(byte[] key)
		{
			using (Aes aes = new AesCryptoServiceProvider())
			{
				aes.Key = key;
				aes.IV = extData;
				
				// Decrypt the message
				using (MemoryStream plaintext = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write))
					{
						cs.Write(msg, 0, msg.Length);
						cs.Close();
						msg = plaintext.ToArray();
					}
				}
			}
		}
		
		public string GetString()
		{
			return Encoding.UTF8.GetString(msg);
		}
		
		public byte[] msg
		{
			get { return _msg; }
			set { _msg = value; }
		}
		
		public int mode
		{
			get { return _mode; }
			set { _mode = value; }
		}
		
		public byte[] extData
		{
			get { return _extData; }
			set { _extData = value; }
		}
	}
}
````

Cấu trúc thông điệp cũng đã được tích hợp luôn hàm ``Encrypt()`` và ``Decrypt()`` vào, trước khi gửi thông điệp đi thì ta cần gọi hàm ``Encrypt()`` và sau khi nhận thông điệp thì ta cần gọi hàm ``Decrypt()``.
