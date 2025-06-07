const User = require('../models/User');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcryptjs');

exports.register = async (req, res) => {
  try {
    const { username, password } = req.body;

    // Verificar se o usuário já existe
    if (await User.findOne({ username })) {
      return res.status(400).json({ error: 'Nome já cadastrado!' });
    }

    // CORREÇÃO: Fazer o hash da password antes de criar o novo utilizador
    const passwordHash = await bcrypt.hash(password, 10);
    const user = new User({ username, passwordHash });
    await user.save();

    // Gerar token JWT
    const token = jwt.sign({ userId: user._id }, process.env.JWT_SECRET, { expiresIn: '1h' });
    res.status(201).json({ token, userId: user._id });
  } catch (error) {
    console.error("Erro detalhado no registo:", error);
    res.status(500).json({ error: 'Erro no registro' });
  }
};

exports.login = async (req, res) => {
  try {
    const { username, password } = req.body;
    const user = await User.findOne({ username });

    if (!user || !(await bcrypt.compare(password, user.passwordHash))) {
      return res.status(401).json({ error: 'Credenciais inválidas!' });
    }

    const token = jwt.sign({ userId: user._id }, process.env.JWT_SECRET, { expiresIn: '1h' });
    res.json({ token, userId: user._id, globalScore: user.globalScore });
  } catch (error) {
    console.error(error);  
    res.status(500).json({ error: error.message || 'Erro no login' });
  }
};