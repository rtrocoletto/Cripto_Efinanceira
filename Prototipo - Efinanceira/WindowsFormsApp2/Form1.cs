using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            txtXML.Text = "<loteEventos><evento id = 'ID777'><tagTeste>Teste</TagTeste></evento></loteEventos>";
        }

        public byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private KeyValuePair<byte[], byte[]> EncryptStringToBytes_Aes(string plainText, byte[] Key, ref byte[] IV)
        {
            byte[] encrypted;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.GenerateIV();
                IV = aesAlg.IV;
                aesAlg.Mode = CipherMode.CBC;
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            var combinedKeyIv = new byte[Key.Length + IV.Length];
            Array.Copy(Key, 0, combinedKeyIv, 0, Key.Length);
            Array.Copy(IV, 0, combinedKeyIv, Key.Length, IV.Length);
            return new KeyValuePair<byte[], byte[]>(combinedKeyIv, encrypted);      

        }

        public static byte[] EncriptarPrivateKeyToRSA(byte[] privateKey, X509Certificate2 certificado)
        {
            using (var cryptoProvider = (RSACryptoServiceProvider)certificado.PublicKey.Key)
            {
                //cryptoProvider
                return cryptoProvider.Encrypt(privateKey, false);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //cria a chave randomica de 16bytes
            RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
            byte[] key = new byte[16];
            random.GetBytes(key);

            byte[] retIV = null; //irá retornar byRef na chamada da função            

            //passa o XML e a chave randomica como parametro
            //retorno
            //retornoLoteEncriptado.Key   ==> Chave (Key + IV)
            //retornoLoteEncriptado.Value ==> Lote Criptografado
            KeyValuePair<byte[], byte[]> retornoLoteEncriptado = EncryptStringToBytes_Aes(txtXML.Text, key, ref retIV);

            //converte para base64 e define conteúdo da tag LOTE
            string lote = Convert.ToBase64String(retornoLoteEncriptado.Value);
            txtLote.Text = lote;

            //instancia o certificado de envio
            //copiar o certificado de preprod para C:\Temp\
            X509Certificate2 cert = new X509Certificate2("C:\\temp\\preprod-efinancentreposto.receita.fazenda.gov.br.cer");

            //chave utilizada para descriptografar o xml enviado para a receita
            byte[] tagChave = EncriptarPrivateKeyToRSA(retornoLoteEncriptado.Key, cert);

            //converte para base64 e define conteúdo da tag CHAVE
            string chave = Convert.ToBase64String(tagChave);
            txtChave.Text = chave;
            
        }

    }
}
