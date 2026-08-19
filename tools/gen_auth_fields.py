# -*- coding: utf-8 -*-
"""
docs/authfinancialapprove.csv dosyasindan AuthMessageFields (Domain) ve
AuthMessageFieldsDto (Application) siniflarini uretir.

Kullanim:  python tools/gen_auth_fields.py
"""
import io, os, re, sys

sys.stdout.reconfigure(encoding="utf-8")

CSV = "docs/authfinancialapprove.csv"
DOMAIN_OUT = "Backend/FraudGuard.Domain/DomainObjects/TransactionProcessing/AuthMessageFields.cs"
DTO_OUT = "Backend/FraudGuard.Application/DTOs/TransactionProcessing/AuthMessageFieldsDto.cs"

# NUMBER(1) oldugu halde bayrak DEGIL, sayili deger tasiyan kolonlar.
# Yorum metinlerinden dogrulandi; bool? yapilirsa 1 ve 2 ayirt edilemez.
NOT_A_FLAG = {
    "PROVISIONCURRENCYTYPE",        # (1:YI, 2:YD)
    "TERMTYPE",                     # ATM = 0, POS = 1 ...
    "PINENTRYMODE",                 # POS sifre giris yetenegi
    "POSTERMINPUTCAPABILITY",       # BKM F061-SF11
    "TRANSACTIONPINSTATUS",         # offline pin dogrulama sonucu
    "CRYPTOGRAMVERIFICATIONRESULT", # CCM dogrulama sonucu
    "TRANSACTIONDESTINATIONTYPE",   # 0..3
    "HCETRANSACTIONTYPE",           # enum
}

# Ayracsiz ORACLE adlarini bolmek icin terim sozlugu (uzun terim once denenir).
WORDS = """
ORGSALETRAN ORGTRANSACTION ORIGINALCOMPLETION ORIGINALDOMESTIC
CARDHOLDER CARDIDENTITY THREEDSECURE PREAUTHORIZATION PREAUTHVALID
AUTHENTICATION VERIFICATION CRYPTOGRAM INSTITUTION DESCRIPTION DESTINATION
CONTACTLESS INSTALLMENT ACQUIRER REFERENCE COMPLETION SETTLEMENT INCREMENTAL
CAPABILITY INDICATOR RECURRING MAGNETIC AVAILABLE UNMATCHED PROVISION
TRANSACTION MERCHANT TERMINAL CATEGORY IDENTITY SURCHARGE INTEREST
CURRENCY BILLING DOMESTIC ORIGINAL REQUIRED PRESENT REVERSED REVERSAL
FALLBACK ECOMMERCE DEFERRED ASSIGNED PROGRAM CHANNEL CREATE BRANCH SCREEN
RESPONSE MESSAGE ACCOUNT PARTIAL ADVICE EXPIRE ISSUER SCRIPT LENGTH
INVOICE PAYMENT WALLET DEVICE MOBILE ONLINE HEADER SOURCE OBJECT RECORD
INSERT MONEY WORLD MASKED SINGLE CAPABLE ORDER ENTRY EXIST CHECK VALID
STATUS RESULT AMOUNT NUMBER SUBCODE INNER
CROSS TRACK2 LEVEL LOCAL SALE TRAN CITY NAME CODE DATE TYPE MODE FLAG
FROM SEND SKIP STAN AUTH CARD TERM USER RATE DAY REPEAT REFUND
LOCATION LIMIT COUNT STATE INPUT SECURE TRACE INDIC SUB DATA
BNET STANDIN PSN HCE AFD BKM CVV2 CVC CAVV EMV MIT ECI RRN MSG PIN POS QR
MAIL FEE ID ACTIVATED UNIQUE REQUEST
""".split()
WORDS.sort(key=len, reverse=True)


# Tokenizer'in dogru bolemedigi adlar. Ayracsiz ORACLE adlarinda kelime siniri
# belirsizdir; bu liste istisnalari acikca kayda gecirir.
NAME_OVERRIDES = {
    "ORGTRANSACTIONOBJECTID": "OrgTransactionObjectId",
    "ORGLOCALAMOUNT": "OrgLocalAmount",
    "ORGBILLINGAMOUNT": "OrgBillingAmount",
    "ORGPROVISIONAMOUNT": "OrgProvisionAmount",
    "ORGSALETRANAUTHCODE": "OrgSaleTranAuthCode",
    "ORGSALETRANAMOUNT": "OrgSaleTranAmount",
    "ORGSALETRANRRN": "OrgSaleTranRrn",
    "THREEDSECURE": "ThreeDSecure",
    "THREEDSECURETYPE": "ThreeDSecureType",
    "SINGLETAPCAPABLE": "SingleTapCapable",
    "SINGLETAPINDICATOR": "SingleTapIndicator",
    "SINGLETAPPINREQUEST": "SingleTapPinRequest",
    "PREAUTHORIZATION": "PreAuthorization",
    "PREAUTHVALIDDAYCOUNT": "PreAuthValidDayCount",
    "ORIGINALCOMPLETIONEXPIREDATE": "OriginalCompletionExpireDate",
    "ORIGINALDOMESTICAMOUNT": "OriginalDomesticAmount",
    "CARDIDENTITYID": "CardIdentityId",
    "TRANSACTIONSUBCODE": "TransactionSubCode",
    "CARDHOLDERACTIVATEDTERMLEVEL": "CardHolderActivatedTermLevel",
}

SPECIAL = {"ID": "Id", "RRN": "Rrn", "STAN": "Stan", "PSN": "Psn", "QR": "Qr"}


def tokenize(name):
    """ORACLE adini terimlere boler. Bolunemeyen kalinti oldugu gibi eklenir."""
    out, i = [], 0
    while i < len(name):
        if name[i] == "_":
            i += 1
            continue
        for w in WORDS:
            if name.startswith(w, i):
                out.append(w)
                i += len(w)
                break
        else:
            j = i
            while j < len(name) and name[j] != "_":
                j += 1
            out.append(name[i:j])
            i = j
    return out


def pascal(name):
    if name in NAME_OVERRIDES:
        return NAME_OVERRIDES[name]
    parts = []
    for t in tokenize(name):
        parts.append(SPECIAL.get(t, t[0] + t[1:].lower()))
    return "".join(parts)


def csharp_type(col, oracle):
    if oracle.startswith(("VARCHAR2", "CHAR", "NVARCHAR")):
        return "string?"
    if oracle.startswith(("DATE", "TIMESTAMP")):
        return "DateTime?"
    m = re.match(r"NUMBER\((\d+)(?:,\s*(\d+))?\)", oracle)
    if m:
        precision, scale = int(m.group(1)), int(m.group(2) or 0)
        if scale > 0:
            return "decimal?"
        if precision == 1 and col not in NOT_A_FLAG:
            return "bool?"
        if precision <= 9:
            return "int?"
        return "long?"
    return "string?"


def read_columns():
    raw = io.open(CSV, encoding="iso-8859-9").read().splitlines()
    cols = []
    for line in raw[1:]:
        p = line.split(";")
        if len(p) >= 3 and re.fullmatch(r"[A-Z0-9_]+", p[0].strip()):
            comment = p[-1].strip().replace('"', "").replace("\n", " ")
            comment = re.sub(r"\s+", " ", comment)
            cols.append((p[0].strip(), p[2].strip(), comment))
    return cols


def xml_escape(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def emit(cols, namespace, class_name, summary):
    L = []
    L.append("// <auto-generated>")
    L.append("//     Bu dosya tools/gen_auth_fields.py tarafindan URETILMISTIR.")
    L.append("//     Kaynak: docs/authfinancialapprove.csv")
    L.append("//     ELLE DUZENLEMEYIN - yeniden uretimde degisiklikler kaybolur.")
    L.append("// </auto-generated>")
    L.append("using System;")
    L.append("")
    L.append("namespace %s" % namespace)
    L.append("{")
    L.append("    /// <summary>")
    for line in summary:
        L.append("    /// %s" % line)
    L.append("    /// </summary>")
    L.append("    public class %s" % class_name)
    L.append("    {")
    for i, (col, oracle, comment) in enumerate(cols):
        if i:
            L.append("")
        doc = xml_escape(comment) if comment else "Kaynak kolon: %s" % col
        L.append("        /// <summary>%s</summary>" % doc)
        L.append("        /// <remarks>AUTHFINANCIALAPPROVE.%s (%s)</remarks>" % (col, oracle))
        L.append("        public %s %s { get; set; }" % (csharp_type(col, oracle), pascal(col)))
    L.append("    }")
    L.append("}")
    return "\n".join(L) + "\n"


def main():
    cols = read_columns()
    names = [pascal(c) for c, _, _ in cols]
    dupes = {n for n in names if names.count(n) > 1}
    if dupes:
        print("HATA: yinelenen property adi:", dupes)
        return 1

    domain = emit(
        cols,
        "FraudGuard.Domain.DomainObjects.TransactionProcessing",
        "AuthMessageFields",
        [
            "AUTHFINANCIALAPPROVE yetkilendirme mesajinin ham alanlari.",
            "<para>",
            "Tum alanlar nullable'dir ve bu kasitlidir: <c>null</c> = alan bu islemde",
            "gonderilmedi, <c>false</c> = gonderildi ve hayir. Fraud degerlendirmesinde",
            "\"bilinmiyor\" ile \"hayir\" ayni sey degildir; nullable tipler bu ayrimi",
            "derleme aninda zorunlu kilar (ornegin <c>!input.Auth.PinExist</c> derlenmez,",
            "yazarin <c>input.Auth.PinExist == false</c> demesi gerekir).",
            "</para>",
            "<para>",
            "Kokte karsiligi olan alanlar icin (Amount, Location, RRN, MerchantId, MccKodu)",
            "kural yazarken <b>koku</b> kullanin; buradakiler ham, donusturulmemis degerlerdir.",
            "</para>",
        ],
    )

    dto = emit(
        cols,
        "FraudGuard.Application.DTOs.TransactionProcessing",
        "AuthMessageFieldsDto",
        [
            "Istekte gonderilen ham auth mesaji alanlari.",
            "<see cref=\"FraudGuard.Domain.DomainObjects.TransactionProcessing.AuthMessageFields\"/>",
            "ile birebir ayni sekle sahiptir; AutoMapper konvansiyonla esler.",
        ],
    )

    io.open(DOMAIN_OUT, "w", encoding="utf-8", newline="\r\n").write(domain)
    io.open(DTO_OUT, "w", encoding="utf-8", newline="\r\n").write(dto)

    from collections import Counter
    dist = Counter(csharp_type(c, o) for c, o, _ in cols)
    print("URETILDI: %d alan" % len(cols))
    for k, v in dist.most_common():
        print("   %-11s %3d" % (k, v))
    print("   %s" % DOMAIN_OUT)
    print("   %s" % DTO_OUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
