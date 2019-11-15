import json
import sqlite3
import sys
from twisted.internet.protocol import Protocol, Factory, ServerFactory
from autobahn.twisted.websocket import WebSocketServerProtocol, WebSocketServerFactory
from datetime import datetime
import netifaces as ni

class CsharpProtocol(Protocol):
    def connectionMade(self):
        print("C# baglandı")

    def dataReceived(self, data):

        self.data_json = self.getJson(data)
        komut = self.data_json["komut"]

        if komut == "getRowIdS":
            dataToGo = bytes(json.dumps([{"count": self.getCount(), "rowIdS": self.getRowId()}]) + "\n", "utf-8")
            self.transport.write(dataToGo)

        elif komut == "check":
            dataToGo = bytes("ok" + "\n", "utf-8")
            self.transport.write(dataToGo)
        elif komut == "ekle":
            self.duyuruEkle(self.data_dict["kullanici"], self.data_dict["baslik"], self.data_dict["duyuru"],
                            self.data_dict["giris_tarih"], self.data_dict["cikis_tarih"], self.data_dict["duyuru_tur"])
            dataToGo = bytes(json.dumps([{"status": "ok"}]) + "\n", "utf-8")

            if self.data_dict["duyuru_tur"] == "aktif":
                self.factory.broadcast(json.dumps({"duyuruCheck": True}))
            self.transport.write(dataToGo)

        elif komut == "yenile":
            self.reqRowId = int(self.data_dict["rowid"])
            dataToGo = bytes(json.dumps(self.yenileDuyuru(self.reqRowId)) + "\n", "utf-8")
            self.transport.write(dataToGo)

        elif komut == "sil":
            self.duyuruSil(self.data_dict["rowid"])
            dataToGo = bytes(json.dumps([{"status": "ok"}]) + "\n", "utf-8")
            if self.data_dict["tur"] == "aktif":
                self.factory.broadcast(json.dumps({"duyuruCheck": True}))
            self.transport.write(dataToGo)

        elif komut == "yolla":
            msg = "{} from {}".format(self.data_dict["msg"], self.peer)
            self.factory.broadcast(msg)

        elif komut == "setAyarlar":
            self.ayarGuncelle(self.data_dict["sayfaArkaPlan"], self.data_dict["duyuruArkaPlan"],
                              self.data_dict["metinRengi"],
                              self.data_dict["metinFontu"], self.data_dict["donusSure"])
            dataToGo = bytes(json.dumps([{"status": "ok"}]) + "\n", "utf-8")
            self.transport.write(dataToGo)

            self.factory.broadcast(json.dumps({"ayarCheck": True}))
        elif komut == "getAyarlar":
            dataToGo = bytes(json.dumps({"ayarlar": self.getAyarlar()}) + "\n", "utf-8")
            #self.factory.broadcast("yeniAyar")
            self.transport.write(dataToGo)


    def connectionLost(self, reason):
        print(reason)


    def getJson(self, data):
        self.data_dict = str(data, "utf-8")
        self.data_dict = json.loads(self.data_dict)
        return self.data_dict

    def getAyarlar(self):
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('SELECT * FROM ayarlar')
            row = c.fetchone()
            rowsList = [row[0], row[1], row[2], row[3], row[4]]
            # dataToGO = bytes(json.dumps(rowsListed) + "\n", "utf-8")
            return rowsList

        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def ayarGuncelle(self, sayfaArkaRenk, duyuruArkaRenk, metinRenk, metinFont, interval):
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('DELETE FROM ayarlar')
            c.execute('INSERT INTO ayarlar VALUES (?, ?, ?, ?, ?)',
                      [sayfaArkaRenk, duyuruArkaRenk, metinRenk, metinFont, interval])
            conn.commit()
            conn.close()
        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def duyuruEkle(self, kullanici, baslik, duyuru, giris_tarih, cikis_tarih, duyuru_tur):
        # DUYURU EKLEME
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('INSERT INTO duyuru VALUES (?, ?, ?, ?, ?, ? )',
                      [kullanici, baslik, duyuru, giris_tarih, cikis_tarih, duyuru_tur])
            conn.commit()
            conn.close()
        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def yenileDuyuru(self, reqRowId):

        # tablonun son halini alıp, json a cevirip, sonra byte a ceviriyor.
        #PATLAYACAK MUHTEMELEN VERI SILININCE...
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('SELECT rowid, * FROM duyuru WHERE rowid = (?)', [reqRowId])
            rows = c.fetchall()
            rowsListed = [{"rowid": row[0], "kullanici": row[1], "baslik": row[2], "duyuru": row[3],
                           "giris_tarih": row[4], "cikis_tarih": row[5], "duyuru_tur": row[6]} for row in rows]

            #dataToGO = bytes(json.dumps(rowsListed) + "\n", "utf-8")
            return rowsListed

        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def duyuruSil(self, reqRowId):
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('DELETE FROM duyuru WHERE rowid = (?)', [reqRowId])
            conn.commit()
            conn.close()
        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def getCount(self):
        conn = sqlite3.connect('duyuruDB.db')
        c = conn.cursor()
        c.execute('SELECT count(*) FROM duyuru')
        count = c.fetchone()
        return [count[0]]

    def getRowId(self):
        conn = sqlite3.connect('duyuruDB.db')
        c = conn.cursor()
        c.execute('SELECT rowid FROM duyuru')
        rowIdS = c.fetchall()
        rowIdS = [k[0] for k in rowIdS]

        return rowIdS


class ClientHtmlProtocol(WebSocketServerProtocol):
    def onOpen(self):

        #if len(self.factory.clients) != 0:
        #    self.factory.clients = []

        self.factory.register(self)

    def onMessage(self, payload, isBinary):
        komut = payload.decode('utf8')

        if komut == "aktif":
            duyurular = self.aktifDuyurular()
            duyurular = json.dumps({"duyurular": duyurular})
            self.sendMessage(duyurular.encode('utf8'))
            #self.factory.broadcast(duyurular)
        elif komut == "ayarlar":

            #burdaki ve yukardaki broadcast birden fazla client.html bagladıgımız zaman sacmaliyor. daha sonra düzelt buraları
            #şimdilik tek bir client.html e izin ver.
            ayarlar = self.getAyarlar()
            ayarlar = json.dumps({"ayarlar": ayarlar})
            self.sendMessage(ayarlar.encode('utf8'))
            #self.factory.broadcast(ayarlar)

        elif komut == "ip":
            addr = self.getIp();
            addr = json.dumps({"ip": addr})
            self.sendMessage(addr.encode('utf8'))

    def getIp(self):
        return ni.ifaddresses('{DB2FCC38-FA39-4FAA-9E4C-EECB32FB6D9E}')[2][0]['addr']

    def connectionLost(self, reason):

        WebSocketServerProtocol.connectionLost(self, reason)
        print("Losing my mind")
        self.factory.unregister(self)

    def getAyarlar(self):
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('SELECT * FROM ayarlar')
            row = c.fetchone()
            rowsList = [row[0], row[1], row[2], row[3], row[4]]
            # dataToGO = bytes(json.dumps(rowsListed) + "\n", "utf-8")
            return rowsList

        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def aktifDuyurular(self):
        conn = sqlite3.connect('duyuruDB.db')
        c = conn.cursor()

        c.execute('SELECT * FROM duyuru WHERE duyuru_tur = (?)', ['aktif'])
        rows = c.fetchall()
        rowsListed = [row[2] for row in rows]
        return rowsListed


class BroadcastServerFactory(WebSocketServerFactory):
    """
    Gelen tüm mesajları bağlı olan tüm clientlara yollayan Broadcast Server
    """

    clients = []

    def __init__(self, url):
        WebSocketServerFactory.__init__(self, url)
        # self.clients = []
        self.veriGüncelle()

    def register(self, client):

        if not client in self.clients:
            print("registered client {}".format(client.peer))
            self.clients.append(client)

    def unregister(self, client):
        if client in self.clients:
            print("unregistered client {}".format(client.peer))
            self.clients.remove(client)

    def broadcast(self, msg):

        if len(self.clients) != 0:
            print("broadcasting message '{}' ..".format(msg))

            for c in self.clients:

                c.sendMessage(msg.encode('utf8'))
                print("message sent to {}".format(c.peer))

    def veriGüncelle(self):

        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            c.execute('SELECT rowid, * FROM duyuru')
            rows = c.fetchall()
            aktifHaleGelecekDuyurular = []
            silinecekDuyurular = []
            for row in rows:
                giris_tarih = datetime.strptime(row[4], "%d.%m.%Y")
                cikis_tarih = datetime.strptime(row[5], "%d.%m.%Y")
                if row[6] == "plan":
                    if giris_tarih <= datetime.now():
                        aktifHaleGelecekDuyurular.append(row[0])
                        print("'{0}' aktif hale gelecek. rowId = {1}".format(row[3], row[0]))
                if datetime.now() > cikis_tarih:
                    silinecekDuyurular.append(row[0])
                    print("'{0}' silinecek. rowId = {1}".format(row[3], row[0]))

            self.duyuruGüncelAktif(aktifHaleGelecekDuyurular)
            self.duyuruGuncelSil(silinecekDuyurular)
        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def duyuruGuncelSil(self, rowsList):
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            for rowId in rowsList:
                c.execute('DELETE FROM duyuru WHERE rowid = (?)', [rowId])
                print("{0} rowID li duyuru silindi".format(rowId))
            conn.commit()
            conn.close()
        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])

    def duyuruGüncelAktif(self, rowsList):
        try:
            conn = sqlite3.connect('duyuruDB.db')
            c = conn.cursor()
            for rowId in rowsList:
                c.execute('UPDATE duyuru SET duyuru_tur = (?) WHERE rowid = (?)', ['aktif', rowId])
                print("{0} rowID li duyuru aktif hale geldi".format(rowId))
            conn.commit()
            conn.close()
        except sqlite3.OperationalError:
            print("Operational Error:", sys.exc_info()[1])


def main():
    import sys
    from twisted.python import log
    from twisted.internet import reactor

    log.startLogging(sys.stdout)

    csharpFactory = BroadcastServerFactory(u"ws://127.0.0.1:9000")
    csharpFactory.protocol = CsharpProtocol

    clientHtmlFactory = BroadcastServerFactory(u"ws://127.0.0.1:9001")
    clientHtmlFactory.protocol = ClientHtmlProtocol
    reactor.listenTCP(9000, csharpFactory)
    reactor.listenTCP(9001, clientHtmlFactory)
    reactor.run()


if __name__ == '__main__':
    main()