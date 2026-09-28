# Raspberry Pi での自動起動

## 1. OS 設定

### コンソールの自動ログイン

**設定**

```bash
sudo raspi-config nonint do_boot_behaviour B2
```

### グループ

- `video`・`render`・`input`・`gpio`・`dialout` があること

**追加する場合**

```bash
sudo usermod -aG video,render,input,gpio,dialout <ユーザー>
```

## 2. デプロイ

### 配置

```bash
dotnet publish src/Template.EmbeddedApp/Template.EmbeddedApp.csproj -c Release -f net10.0 -r linux-arm64 -p:PublishSingleFile=true --self-contained -o publish
ssh pi@<端末> mkdir -p Template.EmbeddedApp
scp publish/* pi@<端末>:Template.EmbeddedApp/
ssh pi@<端末> chmod +x Template.EmbeddedApp/Template.EmbeddedApp
```

- 設定: `~/Template.EmbeddedApp/appsettings.Production.json`

## 3. 自動起動

`~/.config/systemd/user/template-embeddedapp.service` を作成。

```ini
[Unit]
Description=Template.EmbeddedApp

[Service]
Type=simple
WorkingDirectory=%h/Template.EmbeddedApp
ExecStart=%h/Template.EmbeddedApp/Template.EmbeddedApp
Environment=DOTNET_ENVIRONMENT=Production
KillSignal=SIGINT
Restart=always
RestartSec=10

[Install]
WantedBy=default.target
```

### 有効に

```bash
systemctl --user daemon-reload
systemctl --user enable --now template-embeddedapp
```

### 停止・開始

```bash
systemctl --user stop template-embeddedapp
systemctl --user start template-embeddedapp
```

## 4. キーボード

キーボードを繋ぐとき tty1 のシェルに入力が届かないようにする。

**設定**

```bash
cat >> ~/.profile <<'EOF'
if [ "$(tty)" = "/dev/tty1" ]; then
    exec sleep infinity
fi
EOF
```

## 5. ディスプレイ

解像度が合わないとき EDID を固定する。

**設定**

```bash
sudo mkdir -p /lib/firmware/edid
sudo cp /sys/class/drm/card1-HDMI-A-1/edid /lib/firmware/edid/display.bin
sudo cp /boot/firmware/cmdline.txt /boot/firmware/cmdline.txt.bak
sudo sed -i '1s|$| drm.edid_firmware=HDMI-A-1:edid/display.bin|' /boot/firmware/cmdline.txt
sudo reboot
```

- 確認: 起動のログの `Display: size=[...]`(`Display` 節の `Width` / `Height` と違うと警告が出る)
