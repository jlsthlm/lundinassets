#!/bin/bash
# LundinAssets Deployment Script
# Run from your LOCAL machine (not the VPS) after building.
# Usage: ./deploy.sh [vps-user@host]
#
# Prerequisites on VPS:
#   apt install dotnet-runtime-8.0 aspnetcore-runtime-8.0 nginx mysql-server certbot python3-certbot-nginx

set -e

VPS="${1:-user@topward.se}"
DEPLOY_DIR="/var/www/lundinassets"
PUBLISH_DIR="./publish"
SERVICE="lundinassets"

echo "==> Building release..."
dotnet publish ../LundinAssets.csproj -c Release -o "$PUBLISH_DIR" --no-self-contained

echo "==> Uploading to $VPS:$DEPLOY_DIR ..."
ssh "$VPS" "sudo mkdir -p $DEPLOY_DIR && sudo chown \$USER:\$USER $DEPLOY_DIR"
rsync -avz --delete "$PUBLISH_DIR/" "$VPS:$DEPLOY_DIR/"

echo "==> Setting permissions..."
ssh "$VPS" "
  sudo chown -R www-data:www-data $DEPLOY_DIR
  sudo chmod -R 755 $DEPLOY_DIR
  sudo mkdir -p $DEPLOY_DIR/wwwroot/uploads/assets
  sudo mkdir -p $DEPLOY_DIR/wwwroot/uploads/profiles
  sudo chown -R www-data:www-data $DEPLOY_DIR/wwwroot/uploads
"

echo "==> Installing systemd service..."
scp lundinassets.service "$VPS:/tmp/lundinassets.service"
ssh "$VPS" "
  sudo cp /tmp/lundinassets.service /etc/systemd/system/lundinassets.service
  sudo systemctl daemon-reload
  sudo systemctl enable $SERVICE
  sudo systemctl restart $SERVICE
  sudo systemctl status $SERVICE --no-pager
"

echo "==> Installing nginx config..."
scp nginx.conf "$VPS:/tmp/lundinassets-nginx.conf"
ssh "$VPS" "
  sudo cp /tmp/lundinassets-nginx.conf /etc/nginx/sites-available/lundinassets
  sudo ln -sf /etc/nginx/sites-available/lundinassets /etc/nginx/sites-enabled/lundinassets
  sudo nginx -t && sudo systemctl reload nginx
"

echo ""
echo "==> Deployment complete!"
echo "    Site: http://lundinassets.topward.se"
echo ""
echo "==> To enable HTTPS, run on VPS:"
echo "    sudo certbot --nginx -d lundinassets.topward.se"
echo ""
echo "==> MySQL setup (run once on VPS):"
echo "    sudo mysql -e \"CREATE DATABASE lundinassets CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;\""
echo "    sudo mysql -e \"CREATE USER 'lundin_user'@'localhost' IDENTIFIED BY 'CHANGE_ME';\""
echo "    sudo mysql -e \"GRANT ALL PRIVILEGES ON lundinassets.* TO 'lundin_user'@'localhost';\""
echo "    sudo mysql -e \"FLUSH PRIVILEGES;\""
