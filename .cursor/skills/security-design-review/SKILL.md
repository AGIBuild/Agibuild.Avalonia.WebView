---
name: security-design-review
description: Reviews designs and business goals for security vulnerabilities, data protection (in transit/at rest), authorization, and compliance alignment. Use when the user asks for a security review, threat modeling, attack surface analysis, data leakage prevention, or compliance/security assessment.
---

# Security Design Review (Senior Security Expert)

## Scope

Review the provided design proposal or business goals for:
- Potential vulnerabilities and attack surfaces (injection, auth/authz, data leakage, misconfiguration, supply chain).
- Data transmission and storage security controls.
- Compliance/industry standard alignment (only when applicable; state assumptions).
- Risk mitigation strategies and residual risk.

## Operating Principles

- Use the information provided. If critical information is missing, call it out explicitly as a risk/unknown and list the minimum questions needed.
- Prefer concrete, actionable mitigations tied to specific components/flows.
- Avoid generic “best practices” unless mapped to a finding.
- Do not invent compliance requirements; state what standards apply *conditionally*.

## Review Workflow

1. **System context**
   - Identify assets (PII/secrets/credentials), actors, entry points, trust boundaries.
   - Note deployment model (desktop/web/mobile, on-prem/cloud), and key dependencies.

2. **Attack surface & threat analysis**
   - Threats by category: spoofing, tampering, repudiation, information disclosure, DoS, elevation of privilege.
   - Focus on: authentication, authorization, session/token handling, input validation, deserialization, file upload, SSRF, XSS/CSRF, command injection, path traversal, dependency risks.

3. **Data protection**
   - In transit: TLS version/pinning (if relevant), mTLS (if needed), HSTS, secure cookies, API auth.
   - At rest: encryption, key management (KMS/HSM), secret storage, backup protection, least privilege access to storage.
   - Logging/telemetry: avoid sensitive data, retention and access controls.

4. **Compliance & standards (conditional)**
   - Map findings to relevant references where appropriate: OWASP Top 10, OWASP ASVS, NIST 800-53/63, ISO 27001, SOC 2, PCI DSS, GDPR/CCPA, local regulations.
   - Clearly state applicability assumptions (industry, geography, data types).

5. **Mitigation plan**
   - Provide prioritized fixes (P0/P1/P2), owners/components, and verification methods (tests, scans, threat model updates).

## Risk Rating Guidance

Assign **High / Medium / Low** using:
- **Impact**: data sensitivity, blast radius, financial/legal exposure, availability.
- **Likelihood**: ease of exploitation, exposure (internet-facing), required privileges, existing controls.

## Output Report Template (Chinese)

Use the following structure verbatim.

### 1) 执行摘要
- **总体风险等级**：高 / 中 / 低
- **最关键的 3 个风险**：
  - 风险 1（一句话）
  - 风险 2（一句话）
  - 风险 3（一句话）
- **最优先的 3 个改进**：
  - 改进 1（对应风险/组件）
  - 改进 2（对应风险/组件）
  - 改进 3（对应风险/组件）

### 2) 资产、信任边界与攻击面概览
- **关键资产**：PII/凭证/密钥/业务核心数据/配置/日志等
- **主要入口**：API、WebView、文件导入、IPC、本地存储、第三方回调等
- **信任边界**：客户端 ↔ 服务端、Web 内容 ↔ 宿主、进程间等
- **假设与未知项**（会影响结论的缺失信息）

### 3) 发现的问题与改进措施（逐条）

对每条发现，使用以下字段：
- **标题**：
- **风险等级**：高 / 中 / 低
- **影响范围**：涉及数据/用户/系统组件
- **攻击路径/利用方式**：用 2-5 句描述可行攻击链路
- **根因**：设计/流程/控制缺失点
- **改进措施**：
  - 立即措施（P0）
  - 中期措施（P1）
  - 长期治理（P2，可选）
- **验证方式**：如何证明已修复（测试/扫描/审计/监控）
- **参考**（可选）：OWASP/NIST/ISO/合规条款（仅在适用时）

### 4) 数据传输与存储安全评估
- **传输**：TLS 版本/证书校验/鉴权/重放防护/敏感数据传输策略
- **存储**：加密、密钥管理、访问控制、备份、日志与脱敏
- **结论**：充分 / 部分充分 / 不充分（并说明差距）

### 5) 合规与行业标准匹配度（条件化结论）
- **可能适用的标准/法规**（根据已知信息）
- **差距点**：哪些控制缺失会导致不满足
- **需要业务确认的信息**：地域、数据类型、支付场景、审计要求等

### 6) 风险缓解路线图
- **P0（本迭代必须）**：
- **P1（下个迭代）**：
- **P2（季度内）**：
- **残余风险**：修复后仍需接受/转移/监控的风险

