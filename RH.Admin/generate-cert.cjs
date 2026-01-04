const https = require('https');
const fs = require('fs');
const { execSync } = require('child_process');

console.log('正在生成自签名证书...');

const certPath = './dev-cert.pem';
const keyPath = './dev-key.pem';

// 使用Node.js的crypto模块生成自签名证书
const crypto = require('crypto');

// 生成私钥
const privateKey = crypto.generateKeyPairSync('rsa', {
  modulusLength: 2048,
  publicKeyEncoding: {
    type: 'spki',
    format: 'pem'
  },
  privateKeyEncoding: {
    type: 'pkcs8',
    format: 'pem'
  }
});

// 创建证书
const cert = crypto.createCertificate({
  publicKey: privateKey.publicKey,
  signAlgorithm: 'SHA256',
  days: 365,
  extensions: [
    {
      name: 'basicConstraints',
      ca: true,
      critical: true
    },
    {
      name: 'keyUsage',
      keyCertSign: true,
      digitalSignature: true,
      keyEncipherment: true,
      critical: true
    },
    {
      name: 'extKeyUsage',
      serverAuth: true,
      clientAuth: true
    },
    {
      name: 'subjectAltName',
      altNames: [
        { type: 2, value: 'localhost' },
        { type: 2, value: '127.0.0.1' },
        { type: 2, value: '192.168.1.113' },
        { type: 7, ip: '127.0.0.1' },
        { type: 7, ip: '192.168.1.113' }
      ]
    }
  ]
});

// 签署证书
const certificate = crypto.createCertificate({
  ...cert,
  issuer: {
    ...cert.issuer,
    commonName: 'localhost'
  }
});

// 自签名
const signedCert = crypto.createSign('SHA256')
  .update(certificate.toString())
  .end(privateKey.privateKey, 'base64');

// 保存证书和私钥
fs.writeFileSync(certPath, certificate.toString());
fs.writeFileSync(keyPath, privateKey.privateKey);

console.log('✅ 证书生成成功！');
console.log(`📜 证书文件: ${certPath}`);
console.log(`🔑 私钥文件: ${keyPath}`);
console.log('');
console.log('支持的域名和IP:');
console.log('  - localhost');
console.log('  - 127.0.0.1');
console.log('  - 192.168.1.113');
console.log('');
console.log('现在可以使用以下地址访问:');
console.log('  - https://localhost:3000');
console.log('  - https://127.0.0.1:3000');
console.log('  - https://192.168.1.113:3000');
