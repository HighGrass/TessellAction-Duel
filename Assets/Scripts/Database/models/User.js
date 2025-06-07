const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');

const UserSchema = new mongoose.Schema({
  username: { type: String, required: true, unique: true },
  passwordHash: { type: String, required: true },
  globalScore: { type: Number, default: 0 },
  gamesPlayed: { type: Number, default: 0 },
  gamesWon: { type: Number, default: 0 }
});

UserSchema.pre('save', async function(next) {
  if (!this.isModified('passwordHash')) return next();
  next();
});

module.exports = mongoose.model('User', UserSchema);