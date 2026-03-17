using System;
using System.Security.Cryptography;
using System.Text;

namespace BookNumAPI.Services
{
    public class NewebPayService
    {
        private readonly string MerchantID = ""; // 商店代號
        private readonly string HashKey = ""; // 金鑰 (32字元)
        private readonly string HashIV = ""; // IV (16字元)

        /// <summary>
        /// 建立訂單並回傳加密後的 TradeInfo 與 TradeSha
        /// </summary>
        public (string TradeInfo, string TradeSha) CreateOrder(string orderNo, int amount, string itemDesc)
        {
            // 組合交易字串 (必須用 URL 格式，ItemDesc 要做 URL Encode)
            var tradeInfo = $"MerchantID={MerchantID}&RespondType=JSON&TimeStamp={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}&Version=2.0&MerchantOrderNo={orderNo}&Amt={amount}&ItemDesc={Uri.EscapeDataString(itemDesc)}&ReturnURL=https://booknum.com/Payment/Return\r\n&NotifyURL=https://booknum.com/Payment/Notify";

            // AES 加密 (回傳 Hex 字串)
            var tradeInfoAES = EncryptAES(tradeInfo, HashKey, HashIV);

            // SHA256 驗證 (HashKey + TradeInfo + HashIV)
            var tradeSha = EncryptSHA256($"HashKey={HashKey}&{tradeInfoAES}&HashIV={HashIV}");

            return (tradeInfoAES, tradeSha);
        }

        /// <summary>
        /// 解密藍新回傳的 TradeInfo
        /// </summary>
        public string DecryptTradeInfo(string tradeInfoHex)
        {
            byte[] encryptedBytes = StringToByteArray(tradeInfoHex);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(HashKey);
            aes.IV = Encoding.UTF8.GetBytes(HashIV);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decrypted);
        }

        private string EncryptAES(string plainText, string key, string iv)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // 藍新要求回傳 Hex 字串 (小寫)
            return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
        }

        private string EncryptSHA256(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }

        private byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}